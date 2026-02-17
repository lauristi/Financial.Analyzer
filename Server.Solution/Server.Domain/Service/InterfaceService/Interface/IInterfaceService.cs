using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;

namespace Server.Api.Domain.Service.InterfaceService.Interface
{
    internal interface IInterfaceService
    {
        public StatementResponse ProcessAllStatments(string statementFilePath, string expenseFilePath, string finalFilePath);
    }
}