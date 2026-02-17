using Server.Api.Domain.Service.ProcessStatementService.Model;
using Server.Api.Domain.Service.ProcessStatementService.OrchestrationContract.Interface;

namespace Server.Api.Domain.Service.ProcessStatementService.OrchestrationContract
{
    public class StatementMapperService : IStatementMapperService
    {
        public List<SpendingData> MapToSpendingData(List<TransactionModel> transactionModels)
        {
            var spendingList = new List<SpendingData>();

            foreach (var raw in transactionModels)
            {
                spendingList.Add(new SpendingData
                {
                    Date = raw.Date.ToString("dd-MM-yyyy"),
                    Subject = raw.Description,
                    Value = raw.Value,
                    IsCredit = false,
                    FinancialType = 0,
                    Bank = raw.OriginBank,
                    Score = null,
                });
            }

            return spendingList;
        }
    }
}