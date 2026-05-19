using Core.HttpHandleResults.Responses;

namespace Server.Web.Services.Interfaces
{
    public interface IFinancialService
    {
        Task<GenericResponseEnvelope<T>> ProcessStatementAsync<T>(MultipartFormDataContent content);

        Task<GenericResponseEnvelope<T>> UploadExpensesAsync<T>(MultipartFormDataContent content);

        Task<GenericResponseEnvelope<T>> UploadExcelAsync<T>(MultipartFormDataContent content);
    }
}