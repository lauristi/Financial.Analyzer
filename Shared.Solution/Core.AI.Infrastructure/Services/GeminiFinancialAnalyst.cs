using Core.AI.Contracts.Interfaces;
using Core.AI.Contracts.Models;
using Core.AI.Infrastructure.Configurations;
using System.Text;
using System.Text.Json;

namespace Core.AI.Infrastructure.Services
{
    public class GeminiFinancialAnalyst : IFinancialAiAnalyst
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        
        // Tente esta URL exata. O sufixo ":generateContent" é obrigatório.
        private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public GeminiFinancialAnalyst(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<TransactionAnalysisResult> AnalyzeTransactionAsync(string description, CancellationToken ct = default)
        {
            var results = await AnalyzeTransactionBatchAsync(new[] { description }, ct);
            return results.FirstOrDefault() ?? throw new Exception("Gemini falhou em retornar um resultado.");
        }
        
        public async Task<IEnumerable<TransactionAnalysisResult>> AnalyzeTransactionBatchAsync(IEnumerable<string> descriptions, CancellationToken ct = default)
        {
            var promptFull = $"{AiPromptsConfiguration.FinancialAnalystSystemPrompt}\n\nTransações: {string.Join(", ", descriptions)}";

            var payload = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new { text = promptFull }
                }
            }
        },
                generationConfig = new
                {
                    temperature = 0.2,
                    // Remova temporariamente o response_mime_type para isolar o problema do 404
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);

            // Verifique se não há espaços na _apiKey vinda do appsettings
            var response = await _httpClient.PostAsync($"{ApiUrl}?key={_apiKey.Trim()}",
                new StringContent(jsonPayload, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetail = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Erro na API do Gemini: {response.StatusCode} - {errorDetail}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            return ParseGeminiResponse(responseBody);
        }


        private IEnumerable<TransactionAnalysisResult> ParseGeminiResponse(string rawJson)
        {
            using var doc = JsonDocument.Parse(rawJson);
            var textResult = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(textResult))
                return new List<TransactionAnalysisResult>();

            // Limpeza de possíveis blocos de código Markdown
            var cleanedJson = textResult.Trim();
            if (cleanedJson.StartsWith("```json"))
                cleanedJson = cleanedJson.Replace("```json", "").Replace("```", "").Trim();
            else if (cleanedJson.StartsWith("```"))
                cleanedJson = cleanedJson.Replace("```", "").Trim();

            return JsonSerializer.Deserialize<List<TransactionAnalysisResult>>(cleanedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<TransactionAnalysisResult>();
        }
    }
}