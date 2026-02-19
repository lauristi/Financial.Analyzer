using Microsoft.AspNetCore.Http;
using Server.Api.Domain.Service.InfrastrutureService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Model;
using Server.Api.Domain.Service.ProcessStatementService.OrchestrationContract.Interface;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;
using Server.Api.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;

public class StatementOrchestratorService : IStatementOrchestratorService
{
    private readonly IStatementMapperService _statementMapperService;
    private readonly IFinancialIntelligenceService _financialIntelligenceService;
    private readonly IStatementService _statementService;
    private readonly IExpenseService _expenseService;
    private readonly IStatementXlsService _statementXlsService;

    public StatementOrchestratorService(

        IStatementMapperService statementMapperService,
        IFinancialIntelligenceService financialIntelligenceService,
        IStatementService stamentService,
        IExpenseService expenseService,
        IStatementXlsService statementXlsService)
    {
        _statementMapperService = statementMapperService;
        _financialIntelligenceService = financialIntelligenceService;
        _statementService = stamentService;
        _expenseService = expenseService;
        _statementXlsService = statementXlsService;
    }

    public async Task<StatementResponse> ExecuteOrchestrationAsync(List<IFormFile> files)
    {
        // 01.Criar uma lista de transações a partir dos arquivos CSV
        List<TransactionModel> transactions = await _statementService.ProcessCsvFilesAsync(files);

        if (!transactions.Any())
        {
            //todo : criar uma exception customizada para isso
        }

        // 02. Mapear as transações para o modelo de dados de gastos
        var allSpending = new List<SpendingData>();
        var statementResponse = new StatementResponse();

        var expenses = await _expenseService.GetAll();
        allSpending = _statementMapperService.MapToSpendingData(transactions);
               

        // 03 - Inteligência Local (Síncrona/Heurística)
        statementResponse = _financialIntelligenceService.AnalyzeSpending(allSpending, expenses);

        // 04 - Inteligência Artificial (Assíncrona/Probabilística)
        // Passamos a lista e o método nos devolve a mesma lista atualizada
        statementResponse.SpendingDataList = await _financialIntelligenceService.AnalyzeSpendingUsingIAAsync(statementResponse.SpendingDataList);


        //04  Cria o XLS
        statementResponse.FilePath = _statementXlsService.CreateStatementExcel(statementResponse.SpendingDataList);

        if (System.IO.File.Exists(statementResponse.FilePath))
        {
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(statementResponse.FilePath);
            statementResponse.FileBase64 = Convert.ToBase64String(fileBytes);
        }

        return statementResponse;
    }
}