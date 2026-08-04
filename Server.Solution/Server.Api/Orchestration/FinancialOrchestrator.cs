using Microsoft.AspNetCore.Http;
using Server.Api.Models;
using Server.Api.Orchestration.Interface;
using Server.Api.Services.Interfaces;

namespace Server.Api.Orchestration
{
    public class FinancialOrchestrator : IFinancialOrchestrator
    {
        private readonly IFinancialIntelligenceService _financialIntelligenceService;
        private readonly IStatementService _statementService;
        private readonly IExpenseService _expenseService;
        private readonly IStatementXlsService _statementXlsService;

        public FinancialOrchestrator(
            IFinancialIntelligenceService financialIntelligenceService,
            IStatementService statementService,
            IExpenseService expenseService,
            IStatementXlsService statementXlsService)
        {
            _financialIntelligenceService = financialIntelligenceService;
            _statementService = statementService;
            _expenseService = expenseService;
            _statementXlsService = statementXlsService;
        }

        public async Task<StatementResponse> ExecuteOrchestrationAsync(List<IFormFile> files)
        {
            // 01. Processa arquivos CSV e gera lista de transações
            List<TransactionModel> transactions = await _statementService.ProcessCsvFilesAsync(files);

            if (!transactions.Any())
            {
                // TODO: Criar uma exceção customizada para isso
                return new StatementResponse();
            }

            // 02. Mapeia transações e obtém despesas fixas
            List<SpendingData> allSpending = MapToSpendingData(transactions);
            var expenses = await _expenseService.GetAll();

            // 03. Aplica a Inteligência Local (Síncrona / Heurística)
            StatementResponse statementResponse = _financialIntelligenceService.AnalyzeSpending(allSpending, expenses);

            // 04. Aplica a Inteligência Artificial (Assíncrona / Probabilística) com Fallback
            try
            {
                statementResponse.SpendingDataList = await _financialIntelligenceService
                    .AnalyzeSpendingUsingIAAsync(statementResponse.SpendingDataList);
            }
            catch (TimeoutException)
            {
                ApplyIAFallback(statementResponse.SpendingDataList, "Tempo limite excedido pelo provedor de IA");
            }
            catch (Exception ex)
            {
                ApplyIAFallback(statementResponse.SpendingDataList, $"Erro técnico no serviço de IA: {ex.Message}");
            }

            // 05. Consolida os totais do Dashboard
            _financialIntelligenceService.GenerateDashboardTotals(statementResponse);

            // 06. Cria a planilha Excel (.xlsx) com os dados finais
            await _statementXlsService.CreatePreFormatedExcelAsync(statementResponse);

            // 07. Carrega o arquivo gerado em Base64
            if (!string.IsNullOrEmpty(statementResponse.FilePath) && System.IO.File.Exists(statementResponse.FilePath))
            {
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(statementResponse.FilePath);
                statementResponse.FileBase64 = Convert.ToBase64String(fileBytes);
            }

            return statementResponse;
        }

        #region Helpers

        private List<SpendingData> MapToSpendingData(List<TransactionModel> transactions)
        {
            return transactions.Select(raw => new SpendingData
            {
                Date = raw.Date.ToString("dd-MM-yyyy"),
                Subject = raw.Description,
                Value = raw.Value,
                IsCredit = false,
                FinancialType = FinancialType.Ignore,
                Bank = raw.OriginBank,
                Score = null
            }).ToList();
        }

        private void ApplyIAFallback(List<SpendingData> list, string reason)
        {
            if (list == null) return;

            foreach (var item in list)
            {
                if (!item.ProcessedByIA)
                {
                    item.SourceRule = $"Fallback: {reason}";
                    item.IAExplanation = $"A categorização automática não foi concluída devido a: {reason}";
                    item.ConfidenceLevel = 0;
                }
            }
        }

        #endregion Helpers
    }
}