using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Server.Api.Domain.Service.BankService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Interface;

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
        [Route("api/statement/uploadExpenses")]
        public async Task<IActionResult> UploadExpenses(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Arquivo inválido.");

            await _expenseService.SaveFileAsync(file);
            return Ok("expenses.csv atualizado com sucesso no servidor");
        }

        [HttpPost]
        [Route("api/statement/uploadStatement")]
        public async Task<IActionResult> UploadStatement(List<IFormFile> files)
        {
            if (files == null || !files.Any()) return BadRequest("Nenhum arquivo enviado.");

            var result = await _statementOrchestratorService.ExecuteOrchestrationAsync(files);

            return Ok(result);
        }
        #endregion "Upload"
    }
}