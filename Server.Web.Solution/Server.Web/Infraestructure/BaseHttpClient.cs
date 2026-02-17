using Core.Infrastructure.Common; 
using Core.Infrastructure.Responses; 

namespace Server.Web.Infrastructure
{
    public abstract class BaseHttpClient
    {
        protected readonly HttpClient _http;

        protected BaseHttpClient(HttpClient http)
        {
            _http = http;
        }

        protected async Task<OperationResult<T>> HandleResponse<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                return OperationResult<T>.Success(data);
            }

            try
            {
                // Captura o erro padronizado vindo do nosso GlobalExceptionMiddleware
                var apiError = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                return OperationResult<T>.Failure(apiError.Errors, apiError.ErrorCode);
            }
            catch
            {
                // Fallback para erros inesperados
                return OperationResult<T>.Failure(new List<string> { "Erro de comunicação com o servidor." }, "HTTP_ERROR");
            }
        }
    }
}