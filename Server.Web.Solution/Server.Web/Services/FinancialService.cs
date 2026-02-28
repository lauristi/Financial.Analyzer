using Server.Web.Infrastructure;
using Server.Web.Services.Interfaces;

public class FinancialService : BaseHttpClient, IFinancialService
{
    public FinancialService(HttpClient http) : base(http)
    {
    }

    /// <summary>
    /// Realiza o upload e o processamento do extrato em uma única operação.
    /// </summary>
    public async Task<ResponseEnvelope<T>> ProcessStatementAsync<T>(MultipartFormDataContent content)
    {
        // Agora utilizamos a rota consolidada e a chamada única.
        // O retorno do PostAsync já contém o JSON do StatementResponse.
        var response = await _http.PostAsync("api/statement/processCsv", content);

        return await HandleResponse<T>(response);
    }

    public async Task<ResponseEnvelope<T>> UploadExcelAsync<T>(MultipartFormDataContent content)
    {
        // Atualizado para a nova rota semântica, se o backend também mudou para api/statement
        var response = await _http.PostAsync("api/statement/processXls", content);

        return await HandleResponse<T>(response);
    }

    public async Task<ResponseEnvelope<T>> UploadExpensesAsync<T>(MultipartFormDataContent content)
    {
        // Atualizado para a nova rota semântica, se o backend também mudou para api/statement
        var response = await _http.PostAsync("api/statement/uploadExpenses", content);

        return await HandleResponse<T>(response);
    }
}