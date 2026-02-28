using Core.Infrastructure.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Server.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ErrorSimulatorController : ControllerBase
    {
        /// <summary>
        /// Simula um erro catastrófico que deve ser capturado pelo Middleware (Status 500).
        /// </summary>
        [HttpGet("exception")]
        public IActionResult ThrowException()
        {
            throw new Exception("Erro fatal imprevisto no servidor!");
        }

        /// <summary>
        /// Simula um erro de negócio simples via OperationResult (Status 400).
        /// </summary>
        [HttpGet("business-error")]
        public IActionResult GetBusinessError()
        {
            var result = ResponseEnvelope<string>.Failure("Este item já foi processado.", "ERR_ALREADY_PROCESSED");

            return BadRequest(new ApiErrorResponse
            {
                Title = "Regra de Negócio",
                Message = result.Message,
                ErrorCode = result.ErrorCode
            });
        }

        /// <summary>
        /// Simula múltiplos erros de validação (Status 400).
        /// </summary>
        [HttpGet("validation-errors")]
        public IActionResult GetMultipleErrors()
        {
            var erros = new List<string>
            {
                "O campo Nome é obrigatório.",
                "O formato do E-mail está incorreto.",
                "A senha deve conter caracteres especiais."
            };

            var result = ResponseEnvelope<bool>.Failure("Foram encontrados erros de validação.", "VALIDATION_001", erros);

            return BadRequest(new ApiErrorResponse
            {
                Title = "Falha na Validação",
                Message = result.Message,
                ErrorCode = result.ErrorCode,
                Errors = result.Errors.ToList()
            });
        }
    }
}