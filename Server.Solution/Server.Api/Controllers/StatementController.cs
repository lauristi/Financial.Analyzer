using Microsoft.AspNetCore.Mvc;
using Server.Api.Domain.Service.InfrastrutureService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Interface;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;

namespace Server.Api.Controllers
{
    public class StatementController : Controller
    {
        private readonly IStatementOrchestratorService _statementOrchestratorService;
        private readonly IExpenseService _expenseService;
        private readonly IStatementXlsService _iStatementXlsService;

        public StatementController(IStatementOrchestratorService statementOrchestratorService,
                                   IExpenseService expenseService,
                                    IStatementXlsService statementXlsService)
        {
            _expenseService = expenseService;
            _statementOrchestratorService = statementOrchestratorService;
            _iStatementXlsService = statementXlsService;
        }

        #region "Upload"

        [HttpPost]
        [Route("api/statement/processCsv")]
        public async Task<IActionResult> UploadAndProcessCsv(List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                return BadRequest(ResponseEnvelope<StatementResponse>.Failure("Nenhum arquivo enviado.", "NO_FILES"));
            }

            //01 O serviço o objeto completo de dados (SpendingDataList, Dashboard, etc.)
            //02 Envelopamos o objeto de dados aqui no Controller
            var result = await _statementOrchestratorService.ExecuteOrchestrationAsync(files);
            var envelope = ResponseEnvelope<StatementResponse>.Success(message: "Extratos processados com sucesso.", value: result);

            return Ok(envelope);
        }

        [HttpPost]
        [Route("api/statement/processXls")]
        public async Task<IActionResult> UploadAndProcessXls(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                // Retornamos o envelope de falha em vez de apenas uma string
                return BadRequest(ResponseEnvelope<string>.Failure("Arquivo inválido.", "FILE_EMPTY"));
            }

            //01 O serviço o objeto completo de dados (SpendingDataList, Dashboard, etc.)
            //02 Envelopamos o objeto de dados aqui no Controller
            var result = await _iStatementXlsService.CreateFinalExcelAsync(file);
            var envelope = ResponseEnvelope<StatementResponse>.Success(message: "Extratos processados com sucesso.", value: result);

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