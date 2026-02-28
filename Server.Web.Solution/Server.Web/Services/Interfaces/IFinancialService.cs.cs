namespace Server.Web.Services.Interfaces
{
    public interface IFinancialService
    {
        Task<ResponseEnvelope<T>> ProcessStatementAsync<T>(MultipartFormDataContent content);

        Task<ResponseEnvelope<T>> UploadExpensesAsync<T>(MultipartFormDataContent content);

        Task<ResponseEnvelope<T>> UploadExcelAsync<T>(MultipartFormDataContent content);
    }
}