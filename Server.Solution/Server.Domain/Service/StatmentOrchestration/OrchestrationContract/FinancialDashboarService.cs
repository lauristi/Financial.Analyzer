using Server.Api.Domain.Service.ProcessStatementService.Enum;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;
using Server.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;

namespace Server.Domain.Service.StatmentOrchestration.OrchestrationContract
{
    public class FinancialDashboarService : IFinancialDashboardService
    {
        public void GerateDashboardTotals(StatementResponse processedData)
        {
            foreach (var item in processedData.SpendingDataList)
            {
                //if (item.FinancialType == FinancialType.Ignore) return;

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
    }
}