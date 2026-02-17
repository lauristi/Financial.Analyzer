using Core.Infrastructure.Common;
using Server.Web.Infrastructure;
using Server.Web.Services.Interfaces;
using Server.Web.Services.Models.GroupedModel;

public class FinancialService : BaseHttpClient, IFinancialService
{
    public FinancialService(HttpClient http) : base(http)
    {
    }

    /// <summary>
    /// Realiza o upload e o processamento do extrato em uma única operação.
    /// </summary>
    public async Task<OperationResult<StatementResult>> ProcessStatementAsync(MultipartFormDataContent content)
    {
        // Agora utilizamos a rota consolidada e a chamada única.
        // O retorno do PostAsync já contém o JSON do StatementResponse.
        var response = await _http.PostAsync("api/statement/uploadStatement", content);

        return await HandleResponse<StatementResult>(response);
    }

    public async Task<OperationResult<bool>> UploadExpensesAsync(MultipartFormDataContent content)
    {
        // Atualizado para a nova rota semântica, se o backend também mudou para api/statement
        var response = await _http.PostAsync("api/statement/uploadExpenses", content);

        return await HandleResponse<bool>(response);
    }
}