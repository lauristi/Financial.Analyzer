using Core.Ai.Agent.Models;
using Core.Ai.Agent.Services.Interfaces;
using Server.Api.Models;
using Server.Api.Orchestration.Interface;
using Server.Api.Services.Interfaces;

namespace Server.Api.Services
{
    public class FinancialIntelligenceService : IFinancialIntelligenceService
    {
        private readonly IExpenseService _expenseService;
        private readonly IAiCoreAgentService _aiAgentService;

        public FinancialIntelligenceService(
            IExpenseService expenseService,
            IAiCoreAgentService aiAgentService)
        {
            _expenseService = expenseService;
            _aiAgentService = aiAgentService ?? throw new ArgumentNullException(nameof(aiAgentService));
        }

        public StatementResponse AnalyzeSpending(List<SpendingData> extractedTransactions, List<Expense> expenses)
        {
            var statementResponse = new StatementResponse();

            foreach (var item in extractedTransactions)
            {
                if (string.IsNullOrWhiteSpace(item.Subject)) continue;

                string subjectUpper = item.Subject.ToUpper().Trim();

                // 1. O sinal monetário original define se é Crédito ou Débito do banco
                item.IsCredit = item.Value > 0;

                // 2. Busca correspondência na planilha de regras (expenses.xls)
                var matchedExpense = DetermineExpenseRule(subjectUpper, expenses);

                if (matchedExpense != null)
                {
                    item.Category = matchedExpense.Group;
                    item.CategoryOwner = matchedExpense.SubGroup;
                    item.SourceRule = "Regra Local (Planilha)";
                }
                else
                {
                    // Não mapeado no Excel -> Fica pendente para a IA analisar
                    item.Category = "???";
                    item.SourceRule = "Pendente de Classificação";
                }

                // 3. Calculo do Score de impacto
                item.Score = CalculateFinancialImpactScore(item.Value);

                // 4. Padroniza o valor de saída para exibição visual no relatório
                if (!item.IsCredit)
                {
                    item.Value = -Math.Abs(item.Value);
                }
            }

            statementResponse.SpendingDataList = extractedTransactions;
            return statementResponse;
        }

        public async Task<List<SpendingData>> AnalyzeSpendingUsingIAAsync(List<SpendingData> spendingList, CancellationToken ct = default)
        {
            // Filtra apenas o que não foi mapeado pelo Excel e não é Movimentação Interna
            var pendingItems = spendingList
                .Where(s => (s.Category == "???" || string.IsNullOrWhiteSpace(s.Category)) &&
                            s.Category?.ToUpper() != "MOVIMENTACAO INTERNA")
                .ToList();

            if (!pendingItems.Any())
                return spendingList;

            try
            {
                string systemPrompt = """
                Atue como um analista financeiro sênior. Você receberá uma lista de descrições de transações bancárias.
                Analise cada item e retorne estritamente um array JSON contendo objetos com as seguintes propriedades:

                - "SuggestedCategory": string contendo a categoria sugerida (Ex: MERCADO, FARMACIA, ALUGUEL, LAZER, TRANSPORTE, etc).
                - "ConfidenceLevel": um valor numérico decimal entre 0.0 e 1.0 representando o nível de certeza.
                - "Reasoning": string explicativa curta do porquê da categoria.

                Não adicione textos explicativos ou blocos de Markdown fora do array JSON.
                """;

                AiAgentResponse<AiTransactionResult> aiResponse = await _aiAgentService.ProcessBatchAsync<SpendingData, AiTransactionResult>(
                    inputs: pendingItems,
                    systemPrompt: systemPrompt,
                    textExtractor: x => x.Subject ?? "Transação desconhecida",
                    cancellationToken: ct
                );

                if (!aiResponse.IsSuccess)
                {
                    throw new Exception(aiResponse.ErrorMessage);
                }

                var aiResults = aiResponse.Data.ToList();

                for (int i = 0; i < pendingItems.Count; i++)
                {
                    if (i < aiResults.Count)
                    {
                        var result = aiResults[i];
                        var item = pendingItems[i];

                        item.Category = result.SuggestedCategory;
                        item.ConfidenceLevel = result.ConfidenceLevel;
                        item.IAExplanation = result.Reasoning;
                        item.ProcessedByIA = true;
                        item.SourceRule = "Usando Serviço de I.A.";
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (var item in pendingItems)
                {
                    item.SourceRule = "Erro no processamento IA";
                    item.IAExplanation = ex.Message;
                }
            }

            return spendingList;
        }

        public void GenerateDashboardTotals(StatementResponse processedData)
        {
            processedData.Dashboard = processedData.SpendingDataList.GroupBy(x => x.Category ?? "Sem categoria")
                                                               .Select(g => new FinancialDashboard{Category = g.Key,
                                                                                                   Total= g.Sum(x => x.Value)
                                                                                                  }).ToList();
        }

        #region Helpers

        private Expense? DetermineExpenseRule(string subjectUpper, List<Expense> expenses)
        {
            return expenses.FirstOrDefault(e =>
                !string.IsNullOrWhiteSpace(e.Origin) &&
                subjectUpper.Contains(e.Origin.ToUpper().Trim()));
        }

        private string CalculateFinancialImpactScore(decimal value)
        {
            decimal absoluteValue = Math.Abs(value);
            if (absoluteValue <= 50) return "BAIXO";
            if (absoluteValue <= 150) return "MÉDIO";
            return "ALTO";
        }

        #endregion Helpers
    }
}