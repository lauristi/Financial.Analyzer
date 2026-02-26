using Core.AI.Contracts.Interfaces;
using Server.Api.Domain.Service.ProcessStatementService.Enum;
using Server.Api.Domain.Service.ProcessStatementService.Model;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;
using Server.Api.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;
using Server.Domain.Service.StatmentOrchestration.OrchestrationContract;
using Server.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;

public class FinancialIntelligenceService : IFinancialIntelligenceService
{
    private readonly IExpenseService _expenseService;
    private readonly IFinancialAiAnalyst _aiAnalyst;
    private readonly IFinancialDashboardService _financialDashboardService;

    // Atualize o construtor para receber ambos os serviços
    public FinancialIntelligenceService(IExpenseService expenseService,
                                        IFinancialAiAnalyst aiAnalyst,
                                        IFinancialDashboardService financialDashboardService)
    {
        _expenseService = expenseService;
        _aiAnalyst = aiAnalyst; // Agora o campo deixará de ser nulo
        _financialDashboardService = financialDashboardService;
    }

    #region Metdos de Analise

    #endregion
    public StatementResponse AnalyzeSpending(List<SpendingData> extractedTransactions, List<Expense> expenses)
    {
        var statementResponse = new StatementResponse();

        foreach (var item in extractedTransactions)
        {
            if (string.IsNullOrWhiteSpace(item.Subject)) continue;

            string subjectUpper = item.Subject.ToUpper().Trim();

            // 01. Definição do Tipo Financeiro
            // BB - Debito é negativo, Crédito é positivo
            // NUbank - somente tem debito , mas o valor é positivo

            item.IsCredit = false;

            item.IsCredit = subjectUpper.Contains("CRÉDITO") ||
                            subjectUpper.Contains("CREDITO") ||
                            subjectUpper.Contains("DEPÓSITO") ||
                            subjectUpper.Contains("DEPOSITO") ||
                            subjectUpper.Contains("DEVOLVIDO");

            // 02. Processamento por tipo de fluxo
            if (item.IsCredit)
            {
                // Se for crédito, decidimos se é um crédito que nos interessa ou se ignoramos ruído
                item.FinancialType = IsInternalMovement(subjectUpper)
                    ? FinancialType.Ignore
                    : FinancialType.UnknownCredit;
            }
            else
            {
                // Se for débito, verificamos se é ruído primeiro
                if (IsInternalMovement(subjectUpper))
                {
                    item.FinancialType = FinancialType.Ignore;
                }
                else
                {
                    // Se for um débito real, buscamos a categoria no CSV
                    var owner = DetermineOwner(subjectUpper, expenses);
                    item.FinancialType = MapToFinancialType(owner, item.IsCredit);
                }
            }

            // 03. Determinação do Dono baseada no seu expenses.csv
            item.Owner = DetermineOwner(subjectUpper, expenses);

            // 04. Cálculo do Score
            item.Score = CalculateFinancialImpactScore(item.Value);

            //05 Ajuste do valor para negativo se for débito
            if (!item.IsCredit)
            {
                item.Value = -Math.Abs(item.Value);
            }
        }

        //06 Com tudo processado , geramos os totais para o dashboard   
        _financialDashboardService.GerateDashboardTotals(statementResponse);

        statementResponse.SpendingDataList = extractedTransactions;
        return statementResponse;
    }

    public async Task<List<SpendingData>> AnalyzeSpendingUsingIAAsync(List<SpendingData> spendingList, CancellationToken ct = default)
    {
        // 1. Filtramos apenas os itens onde a categoria (Owner) ainda está vazia
        var pendingItems = spendingList
            .Where(s => string.IsNullOrWhiteSpace(s.Owner))
            .ToList();

        if (!pendingItems.Any())
            return spendingList;

        try
        {
            // 2. Extraímos apenas as descrições (Subject) para enviar à IA
            var descriptions = pendingItems.Select(x => x.Subject ?? "Transação desconhecida").ToList();

            // 3. Chamamos a infraestrutura de IA para processar o lote de uma só vez
            // Note que aqui já usamos o novo método de lote que otimiza o Docker/Ollama
            var aiResults = (await _aiAnalyst.AnalyzeTransactionBatchAsync(descriptions, ct)).ToList();

            // 4. Mapeamos os resultados de volta para os nossos objetos de domínio
            // Usamos um loop indexado para garantir a correspondência da ordem (conforme o prompt exige)
            for (int i = 0; i < pendingItems.Count; i++)
            {
                if (i < aiResults.Count)
                {
                    var result = aiResults[i];
                    var item = pendingItems[i];

                    item.Owner = result.SuggestedCategory;
                    item.ConfidenceLevel = result.ConfidenceLevel;
                    item.IAExplanation = result.Reasoning;
                    item.ProcessedByIA = true;
                    item.SourceRule = "Usando Serviço de I.A.";
                }
            }
        }
        catch (Exception ex)
        {
            // Em caso de falha na IA, marcamos os itens para que o utilizador saiba no XLS
            foreach (var item in pendingItems)
            {
                item.SourceRule = "Erro no processamento IA";
                item.IAExplanation = ex.Message;
            }

            // Num cenário sénior, poderíamos fazer um log aqui (ex: Serilog)
        }

        return spendingList;
    }


    #region Helpers

    private string? DetermineOwner(string subjectUpper, List<Expense> expenses)
    {
        var match = expenses.FirstOrDefault(e =>
            !string.IsNullOrEmpty(e.Origin) &&
            subjectUpper.Contains(e.Origin.ToUpper().Trim()));

        return match?.Owner?.Trim();
    }

    private bool IsInternalMovement(string subjectUpper)
    {
        // Se for uma devolução, não devemos ignorar, pois queremos contabilizar o crédito
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

    private FinancialType MapToFinancialType(string? owner, bool isCredit)
    {
        switch (owner?.ToUpper())
        {
            case "MERCADO":
                return FinancialType.SupermarketDebit;

            case "FARMACIA":
                return FinancialType.PharmacyDebit;

            case "EXTRA":
                return FinancialType.ExtraDebit;

            default:
                // Este bloco agora captura: owner nulo, vazio ou categorias não mapeadas
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
     

    #endregion Metodos de apoio
}