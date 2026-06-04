using Core.AI.Contracts.Interfaces;
using Core.AI.Contracts.Models;
using Core.AI.Infrastructure.Configurations;
using OllamaSharp;
using OllamaSharp.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.AI.Infrastructure.Services
{
    public class DeepSeekFinancialAnalyst : IFinancialAiAnalyst
    {
        private readonly IOllamaApiClient _ollamaClient;
        private readonly string _modelName;

        public DeepSeekFinancialAnalyst(Uri ollamaUri, string modelName)
        {
            _ollamaClient = new OllamaApiClient(ollamaUri);
            _modelName = modelName;
        }

        public async Task<TransactionAnalysisResult> AnalyzeTransactionAsync(string description, CancellationToken ct = default)
        {
            var results = await AnalyzeTransactionBatchAsync(new[] { description }, ct);
            return results.FirstOrDefault() ?? throw new Exception("DeepSeek falhou em retornar um resultado.");
        }

        public async Task<IEnumerable<TransactionAnalysisResult>> AnalyzeTransactionBatchAsync(IEnumerable<string> descriptions, CancellationToken ct = default)
        {
            try
            {
                _ollamaClient.SelectedModel = _modelName;

                var formattedDescriptions = string.Join("\n", descriptions.Select((d, i) => $"{i + 1}. {d}"));
                var batchPrompt = $"Analise as seguintes transações e retorne um ARRAY de JSON: Transações:\n{formattedDescriptions}";

                var request = new GenerateRequest
                {
                    Model = _modelName,
                    System = AiPromptsConfiguration.FinancialAnalystSystemPrompt +
                             "\nRetorne APENAS o JSON array seguindo o esqueleto, um para cada transação na ordem enviada. Não inclua tags <think> e não coloque explicações.",
                    Prompt = batchPrompt,
                    Stream = false,
                    Options = new RequestOptions
                    {
                        Temperature = 0.0f // Força o modo determinístico para mitigar o raciocínio longo
                    }
                };

                string responseText = string.Empty;
                await foreach (var item in _ollamaClient.GenerateAsync(request, ct))
                {
                    if (item != null && !string.IsNullOrEmpty(item.Response))
                    {
                        responseText += item.Response;
                    }
                }

                // Limpeza preventiva: remove blocos <think>...</think> ou markdown residuais
                string cleanJson = CleanDeepSeekResponse(responseText);

                var results = JsonSerializer.Deserialize<List<TransactionAnalysisResult>>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return results ?? new List<TransactionAnalysisResult>();
            }
            catch (JsonException ex)
            {
                throw new Exception($"Erro ao interpretar o JSON do DeepSeek. Conteúdo bruto recebido: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Falha na comunicação com o Ollama no Windows: {ex.Message}", ex);
            }
        }

        private string CleanDeepSeekResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return "[]";

            // 1. Remove qualquer tag <think>...</think> e seu conteúdo interno, se gerados
            string withoutThink = Regex.Replace(response, @"<think>.*?</think>", "", RegexOptions.Singleline);

            // 2. Localiza apenas o conteúdo do array JSON [ ... ]
            var match = Regex.Match(withoutThink, @"\[.*\]", RegexOptions.Singleline);

            return match.Success ? match.Value.Trim() : withoutThink.Trim();
        }
    }
}