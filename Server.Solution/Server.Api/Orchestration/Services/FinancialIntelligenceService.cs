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

                // 1. Identificação de Crédito vs Débito
                item.IsCredit = subjectUpper.Contains("CRÉDITO") ||
                                subjectUpper.Contains("CREDITO") ||
                                subjectUpper.Contains("DEPÓSITO") ||
                                subjectUpper.Contains("DEPOSITO") ||
                                subjectUpper.Contains("DEVOLVIDO");

                // 2. Classificação do Tipo Financeiro
                if (item.IsCredit)
                {
                    item.FinancialType = IsInternalMovement(subjectUpper)
                        ? FinancialType.Ignore
                        : FinancialType.UnknownCredit;
                }
                else
                {
                    if (IsInternalMovement(subjectUpper))
                    {
                        item.FinancialType = FinancialType.Ignore;
                    }
                    else
                    {
                        var matchedExpense = DetermineExpenseRule(subjectUpper, expenses);

                        if (matchedExpense != null)
                        {
                            item.FinancialType = MapToFinancialType(matchedExpense.Group, item.IsCredit);
                            item.Category = matchedExpense.Group;
                            item.CategoryOwner = matchedExpense.SubGroup;
                            item.SourceRule = "Regra Local (Planilha)";
                        }
                        else
                        {
                            item.FinancialType = FinancialType.UnknownDebit;
                        }
                    }
                }

                // 3. Score de impacto e ajuste de sinal monetário
                item.Score = CalculateFinancialImpactScore(item.Value);

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
            // Filtra itens não categorizados pela regra local e que não foram ignorados
            var pendingItems = spendingList
                .Where(s => string.IsNullOrWhiteSpace(s.Category) && s.FinancialType != FinancialType.Ignore)
                .ToList();

            if (!pendingItems.Any())
                return spendingList;

            try
            {
                string systemPrompt = """
                Atue como um analista financeiro sênior. Você receberá uma lista de descrições de transações bancárias.
                Analise cada item e retorne estritamente um array JSON contendo objetos com as seguintes propriedades:

                - "SuggestedCategory": string contendo a categoria sugerida.
                - "ConfidenceLevel": um valor numérico decimal entre 0.0 e 1.0 representando o nível de certeza (ex: 1.0 para alta, 0.5 para média, 0.2 para baixa).
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
            foreach (var item in processedData.SpendingDataList)
            {
                decimal value = Math.Abs(item.Value);
                if (item.IsCredit)
                {
                    processedData.Dashboard.TotalCredit += value;
                }
                else
                {
                    processedData.Dashboard.TotalDebit += value;
                    switch (item.FinancialType)
                    {
                        case FinancialType.SupermarketDebit:
                            processedData.Dashboard.Supermarket += value;
                            break;
                        case FinancialType.PharmacyDebit:
                            processedData.Dashboard.Pharmacy += value;
                            break;
                        case FinancialType.ExtraDebit:
                            processedData.Dashboard.Extra += value;
                            break;
                    }
                }
            }
        }

        #region Helpers

        private Expense? DetermineExpenseRule(string subjectUpper, List<Expense> expenses)
        {
            return expenses.FirstOrDefault(e =>
                !string.IsNullOrWhiteSpace(e.Origin) &&
                subjectUpper.Contains(e.Origin.ToUpper().Trim()));
        }

        private bool IsInternalMovement(string subjectUpper)
        {
            if (subjectUpper.Contains("DEVOLVIDO"))
            {
                return false;
            }

            var termsToIgnore = new[]
            {
                "APLICAÇÃO", "RESGATE", "INVESTIMENTO", "POUPANÇA", "CDB",
                "SALDO", "S A L D O", "TRANSFERIDO", "PIX TRANSF", "ESTORNO"
            };

            return termsToIgnore.Any(term => subjectUpper.Contains(term));
        }

        private FinancialType MapToFinancialType(string groupName, bool isCredit)
        {
            switch (groupName?.ToUpper().Trim())
            {
                case "MERCADO":
                    return FinancialType.SupermarketDebit;

                case "FARMACIA":
                case "FARMÁCIA":
                    return FinancialType.PharmacyDebit;

                case "EXTRA":
                    return FinancialType.ExtraDebit;

                default:
                    return isCredit ? FinancialType.UnknownCredit : FinancialType.UnknownDebit;
            }
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