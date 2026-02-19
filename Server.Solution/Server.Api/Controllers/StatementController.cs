using Microsoft.AspNetCore.Mvc;
using Server.Api.Domain.Service.ProcessStatementService.Interface;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;

namespace Server.Api.Controllers
{
    public class StatementController : Controller
    {
        private readonly IExpenseService _expenseService;
        private readonly IStatementOrchestratorService _statementOrchestratorService;

        public StatementController(IStatementOrchestratorService statementOrchestratorService,
                                   IExpenseService expenseService)
        {
            _expenseService = expenseService;
            _statementOrchestratorService = statementOrchestratorService;
        }

        #region "Upload"

        [HttpPost]
        [Route("api/statement/uploadStatement")]
        public async Task<IActionResult> UploadStatement(List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                return BadRequest(ResponseEnvelope<StatementResponse>.Failure("Nenhum arquivo enviado.", "NO_FILES"));
            }

            // O serviço retorna os dados puros (SpendingDataList, Dashboard, etc.)
            var result = await _statementOrchestratorService.ExecuteOrchestrationAsync(files);

            // Envelopamos o objeto de dados aqui no Controller
            var envelope = ResponseEnvelope<StatementResponse>.Success(message: "Extratos processados com sucesso.",
                                                                       value: result);

            return Ok(envelope);
        }

        [HttpPost]
        [Route("api/statement/uploadExpenses")]
        public async Task<IActionResult> UploadExpenses(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                // Retornamos o envelope de falha em vez de apenas uma string
                return BadRequest(ResponseEnvelope<string>.Failure("Arquivo inválido.", "FILE_EMPTY"));
            }

            await _expenseService.SaveFileAsync(file);
            return Ok(ResponseEnvelope<string>.Success("expenses.csv atualizado com sucesso no servidor"));
        }

        #endregion "Upload"
    }
}