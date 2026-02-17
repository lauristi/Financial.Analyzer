using Core.Infrastructure.Common;
using Server.Web.Services.Models.GroupedModel;

namespace Server.Web.Services.Interfaces
{
    public interface IFinancialService
    {
        Task<OperationResult<StatementResult>> ProcessStatementAsync(MultipartFormDataContent content);

        Task<OperationResult<bool>> UploadExpensesAsync(MultipartFormDataContent content);
    }
}