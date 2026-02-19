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
        private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

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
            // O Gemini aceita o System Prompt de forma estruturada na requisição
            var payload = new
            {
                system_instruction = new { parts = new { text = AiPromptsConfiguration.FinancialAnalystSystemPrompt } },
                contents = new { parts = new { text = $"Transações: {string.Join(", ", descriptions)}" } },
                generationConfig = new
                {
                    response_mime_type = "application/json", // Força o retorno em JSON nativo
                    temperature = 0.2
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var response = await _httpClient.PostAsync($"{ApiUrl}?key={_apiKey}",
                new StringContent(jsonPayload, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Erro na API do Gemini: {response.ReasonPhrase}");

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            // Aqui extraímos o conteúdo do campo 'text' dentro da estrutura de resposta do Gemini
            // e desserializamos para o nosso TransactionAnalysisResult.
            return ParseGeminiResponse(responseBody);
        }

        private IEnumerable<TransactionAnalysisResult> ParseGeminiResponse(string rawJson)
        {
            // O Gemini retorna o JSON dentro de uma estrutura: candidates[0].content.parts[0].text
            // Precisamos extrair essa string e converter para nossa Lista.
            using var doc = JsonDocument.Parse(rawJson);
            var textResult = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            return JsonSerializer.Deserialize<List<TransactionAnalysisResult>>(textResult!, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<TransactionAnalysisResult>();
        }
    }
}