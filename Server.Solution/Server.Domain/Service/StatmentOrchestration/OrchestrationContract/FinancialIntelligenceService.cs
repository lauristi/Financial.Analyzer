using Server.Api.Domain.Service.ProcessStatementService.Enum;
using Server.Api.Domain.Service.ProcessStatementService.Model;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;
using Server.Api.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;

public class FinancialIntelligenceService : IFinancialIntelligenceService
{
    private readonly IExpenseService _expenseService;

    public FinancialIntelligenceService(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

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

            // 05. Acúmulo de Totais
            UpdateTotals(item, statementResponse);

            //06 Ajuste do valor para negativo se for débito
            if (!item.IsCredit)
            {
                item.Value = -Math.Abs(item.Value);
            }
        }

        statementResponse.SpendingDataList = extractedTransactions;
        return statementResponse;
    }

    #region Metodos de apoio

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

    private void UpdateTotals(SpendingData item, StatementResponse totals)
    {
        if (item.FinancialType == FinancialType.Ignore) return;

        if (item.IsCredit)
        {
            totals.Dashboard.TotalCredit += item.Value;
        }
        else
        {
            totals.Dashboard.TotalDebit += item.Value;

            switch (item.FinancialType)
            {
                case FinancialType.SupermarketDebit:
                    totals.Dashboard.Supermarket += item.Value;
                    break;

                case FinancialType.PharmacyDebit:
                    totals.Dashboard.Pharmacy += item.Value;
                    break;

                case FinancialType.ExtraDebit:
                    totals.Dashboard.Extra += item.Value;
                    break;
            }
        }
    }
    
    #endregion Metodos de apoio
}