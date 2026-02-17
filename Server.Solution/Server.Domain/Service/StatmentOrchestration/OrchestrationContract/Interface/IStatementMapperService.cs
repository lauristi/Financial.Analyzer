using Server.Api.Domain.Service.ProcessStatementService.Model;

namespace Server.Api.Domain.Service.ProcessStatementService.OrchestrationContract.Interface
{
    public interface IStatementMapperService
    {
        /// <summary>
        /// Converte a lista de dados brutos para o modelo de domínio SpendingData.
        /// </summary>
        List<SpendingData> MapToSpendingData(List<TransactionModel> transactionModels);
    }
}