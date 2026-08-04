using Server.Api.Models;

namespace Server.Api.Orchestration.Interface
{
    public interface IFinancialIntelligenceService
    {
        StatementResponse AnalyzeSpending(List<SpendingData> extractedTransactions, List<Expense> expenses);

        void GenerateDashboardTotals(StatementResponse processedData);
        
        Task<List<SpendingData>> AnalyzeSpendingUsingIAAsync(List<SpendingData> spendingList, CancellationToken ct = default);
    }
}