using Server.Api.Domain.Service.ProcessStatementService.Model;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;

namespace Server.Api.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface
{
    public interface IFinancialIntelligenceService
    {
        StatementResponse AnalyzeSpending(List<SpendingData> extractedTransactions, List<Expense> expenses);
        Task<List<SpendingData>> AnalyzeSpendingUsingIAAsync(List<SpendingData> spendingList, CancellationToken ct = default);
    }
}