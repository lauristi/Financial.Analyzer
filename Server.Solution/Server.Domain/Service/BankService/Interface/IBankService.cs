using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;

namespace Server.Api.Domain.Service.BankService.Interface
{
    public interface IBankService
    {
        StatementResponse ProcessRawBankDetailsAsync(string statementFilePath, string expenseFilePath, string finalFilePath);
    }
}