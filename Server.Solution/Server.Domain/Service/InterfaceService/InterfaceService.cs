using Server.Api.Domain.Service.InterfaceService.Interface;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;

namespace Server.Api.Domain.Service.InterfaceService
{
    internal class InterfaceService : IInterfaceService
    {
        public StatementResponse ProcessAllStatments(string statementFilePath, string expenseFilePath, string finalFilePath)
        {
            throw new NotImplementedException();
        }
    }
}