using Core.HttpHandleResults.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Core.HttpHandleResults.Middlewares
{
    /// <summary>
    /// Middleware global para captura de exceções.
    /// Funciona como uma malha de segurança que envolve toda a aplicação.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        // O 'RequestDelegate' representa o próximo componente no fluxo (pipeline) da requisição.
        private readonly RequestDelegate _next;

        // Interface para registrar logs no console ou arquivos, essencial para auditoria de erros.
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        // Permite verificar o ambiente (Desenvolvimento, Produção, etc.) para decidir o que exibir.
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Método obrigatório do middleware que é chamado em cada requisição HTTP.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Tenta passar a requisição adiante para o próximo middleware (ou Controller).
                await _next(context);
            }
            catch (Exception ex)
            {
                // Se qualquer erro não tratado ocorrer "acima" no fluxo, ele cairá aqui.
                _logger.LogError(ex, "Ocorreu um erro não tratado detectado pelo Middleware Global.");

                // Inicia o processo de transformar a exceção técnica em uma resposta JSON amigável.
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Constrói a resposta de erro padronizada no formato JSON.
        /// </summary>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Define que o retorno será um JSON e o Status Code será 500 (Erro Interno).
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Criamos o nosso envelope de erro (Contrato de Interface).
            var response = new GenericApiErrorResponse
            {
                Title = "Erro Interno de Servidor",
                Message = "Ocorreu um erro inesperado em nosso sistema. Por favor, tente novamente mais tarde.",
                ErrorCode = "INTERNAL_SERVER_ERROR",

                // O TraceId ajuda a rastrear esta requisição específica nos logs do servidor.
                TraceId = context.TraceIdentifier,

                // Segurança: O detalhe técnico (StackTrace) só é enviado se estivermos em ambiente de Desenvolvimento.
                // Em Produção, o valor será nulo para não expor vulnerabilidades do código.
                TechnicalDetail = _env.IsDevelopment() ? exception.ToString() : null
            };

            // Serializa o objeto para uma string JSON.
            var json = JsonSerializer.Serialize(response);

            // Escreve o JSON diretamente no corpo da resposta HTTP.
            await context.Response.WriteAsync(json);
        }
    }
}