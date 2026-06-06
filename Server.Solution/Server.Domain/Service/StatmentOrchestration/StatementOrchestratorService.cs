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
        // 01. Criar uma lista de transações a partir dos arquivos CSV
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

        // 04 - Inteligência Artificial (Assíncrona/Probabilística) e com Fallback
        try
        {
            // REMOVIDO o .WaitAsync(15). Agora o tempo limite é ditado dinamicamente pela configuração da biblioteca (ex: 90s para o DeepSeek)
            statementResponse.SpendingDataList = await _financialIntelligenceService
                .AnalyzeSpendingUsingIAAsync(statementResponse.SpendingDataList);
        }
        catch (TimeoutException)
        {
            // Mensagem ajustada para refletir o comportamento dinâmico
            ApplyIAFallback(statementResponse.SpendingDataList, "Tempo limite excedido pelo provedor de IA");
        }
        catch (Exception ex)
        {
            ApplyIAFallback(statementResponse.SpendingDataList, $"Erro técnico no serviço de IA: {ex.Message}");
        }

        // 05 - Cria o XLS com os dados atualizados pela IA
        await _statementXlsService.CreatePreFormatedExcelAsync(statementResponse);

        if (System.IO.File.Exists(statementResponse.FilePath))
        {
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(statementResponse.FilePath);
            statementResponse.FileBase64 = Convert.ToBase64String(fileBytes);
        }

        return statementResponse;
    }

    #region Hellper

    private void ApplyIAFallback(List<SpendingData> list, string reason)
    {
        // Usamos o condicional para garantir que não tentamos iterar em lista nula
        if (list == null) return;

        foreach (var item in list)
        {
            // Só marcamos como falha se o item ainda não tiver sido processado com sucesso
            if (!item.ProcessedByIA)
            {
                item.SourceRule = $"Fallback: {reason}";
                item.IAExplanation = $"A categorização automática não foi concluída devido a: {reason}";
                item.ConfidenceLevel = 0;
            }
        }
    }

    #endregion Hellper
}