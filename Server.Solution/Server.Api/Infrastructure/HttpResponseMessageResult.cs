using Microsoft.AspNetCore.Mvc;

namespace Server.Api.Infrastructure
{
    // Classe que implementa a interface IActionResult para retornar um HttpResponseMessage como resultado de uma ação de um controlador
    public class HttpResponseMessageResult : IActionResult
    {
        // Campo privado para armazenar o HttpResponseMessage a ser retornado
        private readonly HttpResponseMessage _httpResponseMessage;

        // Construtor que recebe um HttpResponseMessage e o atribui ao campo privado
        public HttpResponseMessageResult(HttpResponseMessage httpResponseMessage)
        {
            _httpResponseMessage = httpResponseMessage;
        }

        // Método assíncrono que executa o resultado da ação
        public async Task ExecuteResultAsync(ActionContext context)
        {
            // Obtém o objeto Response do HttpContext
            var response = context.HttpContext.Response;

            // Define o código de status da resposta HTTP com base no código de status do HttpResponseMessage
            response.StatusCode = (int)_httpResponseMessage.StatusCode;

            // Copia os cabeçalhos do HttpResponseMessage para a resposta HTTP
            foreach (var header in _httpResponseMessage.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            // Copia os cabeçalhos do conteúdo do HttpResponseMessage para a resposta HTTP
            foreach (var header in _httpResponseMessage.Content.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            // Copia o conteúdo do HttpResponseMessage para o corpo da resposta HTTP
            await _httpResponseMessage.Content.CopyToAsync(response.Body);
        }
    }
}