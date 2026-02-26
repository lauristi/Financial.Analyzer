using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;

namespace Server.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface
{
    public interface IFinancialDashboardService
    {
        void GerateDashboardTotals(StatementResponse processedData);
    }
}