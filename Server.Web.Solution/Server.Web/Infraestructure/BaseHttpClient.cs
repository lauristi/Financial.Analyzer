namespace Server.Web.Infrastructure
{
    public abstract class BaseHttpClient
    {
        protected readonly HttpClient _http;

        protected BaseHttpClient(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Processa a resposta da API esperando sempre um ResponseEnvelope padronizado.
        /// </summary>
        protected async Task<ResponseEnvelope<T>> HandleResponse<T>(HttpResponseMessage response)
        {
            try
            {
                // Se a resposta for sucesso (200-299), lemos o envelope completo do Back-end.
                // O T aqui representa o tipo do campo 'Value' dentro do envelope.
                if (response.IsSuccessStatusCode)
                {
                    var envelope = await response.Content.ReadFromJsonAsync<ResponseEnvelope<T>>();
                    return envelope ?? ResponseEnvelope<T>.Failure("O servidor retornou um corpo vazio.");
                }

                // Se houver erro de negócio ou exceção tratada (ex: 400, 404, 500),
                // o Back-end também enviará um ResponseEnvelope<T> preenchido com as falhas.
                var errorEnvelope = await response.Content.ReadFromJsonAsync<ResponseEnvelope<T>>();
                return errorEnvelope ?? ResponseEnvelope<T>.Failure("Erro desconhecido ao processar a requisição.");
            }
            catch (Exception ex)
            {
                // Fallback para falhas críticas de infraestrutura (ex: JSON malformado ou timeout)
                return ResponseEnvelope<T>.Failure(
                    $"Erro de comunicação: {ex.Message}",
                    "HTTP_COMMUNICATION_ERROR"
                );
            }
        }
    }
}