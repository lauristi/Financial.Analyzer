namespace Core.AI.Infrastructure.Configurations
{
    public static class AiPromptsConfiguration
    {
        /// <summary>
        /// Prompt base para análise de transações financeiras.
        /// </summary>

        // No AiPromptsConfiguration.cs
        public const string FinancialAnalystSystemPrompt = @"Você é um analista financeiro rigoroso.
                                                             Analise as transações enviadas e responda APENAS com um ARRAY de objetos JSON.
                                                             Exemplo de formato esperado:
                                                             [
                                                                { ""suggestedCategory"": ""Saúde"", ""confidenceLevel"": 0.9, ""reasoning"": ""Drogasil identificado"" },
                                                                { ""suggestedCategory"": ""Transporte"", ""confidenceLevel"": 0.8, ""reasoning"": ""Uber identificado"" }
                                                             ]
                                                             Não use markdown, não explique nada.";
        // Adicione ourtros prompts aqui...
        // public const string RiskAnalystPrompt = "...";
    }
}