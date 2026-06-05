using Core.Ai.Agent.Models;
using Core.Ai.Agent.Services.Interfaces;
using Server.Api.Domain.Service.ProcessStatementService.Enum;
using Server.Api.Domain.Service.ProcessStatementService.Model;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;
using Server.Api.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;
using Server.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;

public class FinancialIntelligenceService : IFinancialIntelligenceService
{
    private readonly IExpenseService _expenseService;
    private readonly IAiCoreAgentService _aiAgentService;
    private readonly IFinancialDashboardService _financialDashboardService;

    public FinancialIntelligenceService(IExpenseService expenseService,
                                        IAiCoreAgentService aiAgentService,
                                        IFinancialDashboardService financialDashboardService)
    {
        _expenseService = expenseService;
        _aiAgentService = aiAgentService ?? throw new ArgumentNullException(nameof(aiAgentService));
        _financialDashboardService = financialDashboardService;
    }

    public StatementResponse AnalyzeSpending(List<SpendingData> extractedTransactions, List<Expense> expenses)
    {
        var statementResponse = new StatementResponse();

        foreach (var item in extractedTransactions)
        {
            if (string.IsNullOrWhiteSpace(item.Subject)) continue;

            string subjectUpper = item.Subject.ToUpper().Trim();

            item.IsCredit = false;

            item.IsCredit = subjectUpper.Contains("CRÉDITO") ||
                            subjectUpper.Contains("CREDITO") ||
                            subjectUpper.Contains("DEPÓSITO") ||
                            subjectUpper.Contains("DEPOSITO") ||
                            subjectUpper.Contains("DEVOLVIDO");

            Expense expense = new Expense { Origin = null, Category = null, CategoryOwner = null };

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
                    expense = DetermineOwner(subjectUpper, expenses);
                    item.FinancialType = MapToFinancialType(expense, item.IsCredit);
                }
            }

            expense = DetermineOwner(subjectUpper, expenses);
            item.Category = expense.Category;
            item.CategoryOwner = expense.CategoryOwner;

            item.Score = CalculateFinancialImpactScore(item.Value);

            if (!item.IsCredit)
            {
                item.Value = -Math.Abs(item.Value);
            }
        }

        statementResponse.SpendingDataList = extractedTransactions;
        _financialDashboardService.GerateDashboardTotals(statementResponse);

        return statementResponse;
    }

    public async Task<List<SpendingData>> AnalyzeSpendingUsingIAAsync(List<SpendingData> spendingList, CancellationToken ct = default)
    {
        // 1. Filtramos apenas os itens onde a categoria (Owner) ainda está vazia
        var pendingItems = spendingList
            .Where(s => string.IsNullOrWhiteSpace(s.Category))
            .ToList();

        if (!pendingItems.Any())
            return spendingList;

        try
        {
            // 2. Definição do System Prompt estruturado para forçar o retorno do JSON esperado pela aplicação
            // Usando Raw String Literals (iniciado por três aspas duplas consecutiveis)
            // mantem o texto limpo, idêntico a um arquivo de texto comum

            string systemPrompt = """
            Atue como um analista financeiro sênior. Você receberá uma lista de descrições de transações bancárias.
            Analise cada item e retorne estritamente um array JSON contendo objetos com as seguintes propriedades:

            - "SuggestedCategory": string contendo a categoria sugerida.
            - "ConfidenceLevel": um valor numérico decimal entre 0.0 e 1.0 representando o nível de certeza (ex: 1.0 para alta, 0.5 para média, 0.2 para baixa).
            - "Reasoning": string explicativa curta do porquê da categoria.

            Não adicione textos explicativos ou blocos de Markdown fora do array JSON.
            """;

            // 3. Chamamos o novo motor genérico passando a lista e a expressão lambda que extrai a propriedade 'Subject'
            AiAgentResponse<AiTransactionResult> aiResponse = await _aiAgentService.ProcessBatchAsync<SpendingData, AiTransactionResult>(
                inputs: pendingItems,
                systemPrompt: systemPrompt,
                textExtractor: x => x.Subject ?? "Transação desconhecida",
                cancellationToken: ct
            );

            // 4. Se a biblioteca reportar erro de processamento ou parse, lançamos para o bloco catch tratar
            if (!aiResponse.IsSuccess)
            {
                throw new Exception(aiResponse.ErrorMessage);
            }

            var aiResults = aiResponse.Data.ToList();

            // 5. Mapeamos os resultados estruturados de volta para os nossos objetos de domínio
            for (int i = 0; i < pendingItems.Count; i++)
            {
                if (i < aiResults.Count)
                {
                    var result = aiResults[i];
                    var item = pendingItems[i];

                    item.Category = result.SuggestedCategory;
                    item.ConfidenceLevel = double.Parse(result.ConfidenceLevel ?? "0.0");
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

    #region Helpers

    private Expense DetermineOwner(string subjectUpper, List<Expense> expenses)
    {
        var match = expenses.FirstOrDefault(e =>
            !string.IsNullOrEmpty(e.Origin) &&
            subjectUpper.Contains(e.Origin.ToUpper().Trim()));

        Expense expense = new Expense { Origin = null, Category = null, CategoryOwner = null };

        if (match == null)
            return new Expense { Category = null };

        expense.Origin = match.Origin;
        expense.Category = match.Category;
        expense.CategoryOwner = match.CategoryOwner;

        return expense;
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

    private FinancialType MapToFinancialType(Expense expense, bool isCredit)
    {
        switch (expense.Category?.ToUpper())
        {
            case "MERCADO":
                return FinancialType.SupermarketDebit;

            case "FARMACIA":
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
        if (absoluteValue <= 50)
        {
            return "BAIXO";
        }

        if (absoluteValue > 50 && absoluteValue <= 150)
        {
            return "MÉDIO";
        }

        return "ALTO";
    }

    #endregion Helpers
}

/// <summary>
/// Classe auxiliar interna para mapear o contrato JSON esperado da resposta da Inteligência Artificial.
/// </summary>
public class AiTransactionResult
{
    public string? SuggestedCategory { get; set; }
    public string? ConfidenceLevel { get; set; }
    public string? PointOfAttention { get; set; }
    public string? Reasoning { get; set; }
}