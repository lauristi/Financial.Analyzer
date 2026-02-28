using Core.AI.Contracts.Interfaces;
using Core.AI.Contracts.Models;
using Core.AI.Infrastructure.Configurations;
using Docker.DotNet;
using Docker.DotNet.Models;
using OllamaSharp;
using OllamaSharp.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.AI.Infrastructure.Services
{
    /// <summary>
    /// Implementação do analista financeiro que orquestra o Docker e o Ollama.
    /// Esta classe é a "realização" do contrato definido em Core.AI.Contracts.
    /// </summary>
    public class OllamaFinancialAnalyst : IFinancialAiAnalyst
    {
        private readonly DockerClient _dockerClient;
        private readonly IOllamaApiClient _ollamaClient;
        private const string ContainerName = "ollama";

        /// <summary>
        /// O construtor recebe as configurações necessárias para os clientes externos.
        /// </summary>
        /// <param name="dockerUri">Endereço da API do Docker (ex: npipe://./pipe/docker_engine no Windows).</param>
        /// <param name="ollamaUri">Endereço da API do Ollama (ex: http://localhost:11434).</param>
        public OllamaFinancialAnalyst(Uri dockerUri, Uri ollamaUri)
        {
            _dockerClient = new DockerClientConfiguration(dockerUri).CreateClient();
            _ollamaClient = new OllamaApiClient(ollamaUri);
        }

        /// <summary>
        /// Analisa uma transação utilizando o modelo Phi-3 Mini e gerencia o ciclo de vida do container.
        /// </summary>
        public async Task<TransactionAnalysisResult> AnalyzeTransactionAsync(string description, CancellationToken cancellationToken = default)
        {
            try
            {
                // 01. Acorda a I.A no dokers
                await StartContainerAsync(cancellationToken);

                #region AI Model Interaction

                _ollamaClient.SelectedModel = "phi3:mini";

                // 02. Envia a requisição para o Ollama
                var request = new GenerateRequest
                {
                    Model = _ollamaClient.SelectedModel,                          // Passamos o modelo diretamente no contrato da requisição
                    System = AiPromptsConfiguration.FinancialAnalystSystemPrompt, // Prompt de sistema para orientar o comportamento da IA
                    Prompt = $"Transação: {description}",
                    Stream = false,
                    Options = new OllamaSharp.Models.RequestOptions               // Opcional: para ajustes finos de temperatura
                    {
                        Temperature = 0.3f                                        // Menor temperatura = resposta mais determinística/rígida
                    }
                };

                // 03. Recupera a resposta do modelo (streaming ou completa)
                var responseStream = _ollamaClient.GenerateAsync(request, cancellationToken);
                string responseText = string.Empty;
                await foreach (var item in responseStream.WithCancellation(cancellationToken))
                {
                    if (item != null && !string.IsNullOrEmpty(item.Response))
                    {
                        responseText += item.Response;
                    }
                }

                var result = new { Response = responseText };

                // 04. Limpeza da resposta (Remove possíveis blocos de código markdown)
                string cleanJson = CleanJsonResponse(result.Response);

                // 05. Desserialização para o DTO
                var analysis = JsonSerializer.Deserialize<TransactionAnalysisResult>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                #endregion AI Model Interaction

                return analysis ?? throw new Exception("A IA retornou um resultado vazio.");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Erro ao interpretar o JSON da IA. Conteúdo bruto: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Falha na infraestrutura de IA: {ex.Message}", ex);
            }
            finally
            {
                // 06. Coloca a I.A no dokers para dormir (garante resfrimento)
                await StopContainerAsync();
            }
        }

        /// <summary>
        /// Analisa uma transações em lote utilizando o modelo Phi-3 Mini e gerencia o ciclo de vida do container.
        /// </summary>

        public async Task<IEnumerable<TransactionAnalysisResult>> AnalyzeTransactionBatchAsync(IEnumerable<string> descriptions, CancellationToken cancellationToken = default)
        {
            try
            {
                // 01. Acorda a I.A uma única vez para o lote inteiro
                await StartContainerAsync(cancellationToken);

                // Prepara o prompt com a lista de transações
                // Usamos Join para criar uma string numerada, facilitando para a IA
                var formattedDescriptions = string.Join("\n", descriptions.Select((d, i) => $"{i + 1}. {d}"));
                var batchPrompt = $@"Analise as seguintes transações e retorne um ARRAY de JSON: Transações: {formattedDescriptions}";

                var request = new GenerateRequest
                {
                    Model = "phi3:mini",
                    System = AiPromptsConfiguration.FinancialAnalystSystemPrompt +
                             "\nRetorne um JSON array seguindo o esqueleto, um para cada transação na ordem enviada.",
                    Prompt = batchPrompt,
                    Stream = false,
                    Options = new OllamaSharp.Models.RequestOptions { Temperature = 0.2f }
                };

                // 02. Envia a requisição única
                var responseText = "";
                await foreach (var item in _ollamaClient.GenerateAsync(request, cancellationToken))
                {
                    responseText += item.Response;
                }

                // 03. Limpeza e Desserialização de uma lista
                string cleanJson = CleanJsonResponse(responseText);
                var results = JsonSerializer.Deserialize<List<TransactionAnalysisResult>>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return results ?? new List<TransactionAnalysisResult>();
            }
            finally
            {
                // 04. Coloca a I.A para dormir após processar tudo
                await StopContainerAsync();
            }
        }

        #region Helpers

        /// <summary>
        /// Método auxiliar para extrair apenas o JSON, caso a IA envie markdown ou textos extras.
        /// </summary>
        private string CleanJsonResponse(string response)
        {
            // Procura por conteúdo entre chaves { } caso a IA envie lixo ao redor
            var match = Regex.Match(response, @"\{.*\}", RegexOptions.Singleline);
            return match.Success ? match.Value : response;
        }

        private async Task StartContainerAsync(CancellationToken ct)
        {
            // Nota de Estudo: Aqui o DockerClient verifica o estado atual do container
            // antes de tentar dar o Start, evitando exceções desnecessárias.
            await _dockerClient.Containers.StartContainerAsync(ContainerName, new ContainerStartParameters(), ct);
        }

        private async Task StopContainerAsync()
        {
            // Envia o sinal de parada para o container 'ollama'.
            await _dockerClient.Containers.StopContainerAsync(ContainerName, new ContainerStopParameters { WaitBeforeKillSeconds = 5 });
        }

        #endregion Helpers
    }
}