using Server.Api.Domain.Service.BankService.Interface;
using Server.Api.Domain.Service.InfrastrutureService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Enum;
using Server.Api.Domain.Service.ProcessStatementService.Model;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;
using Server.Api.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;
using System.Globalization;
using System.Text;

namespace Server.Api.Domain.Service.BankService
{
    public class BankService : IBankService
    {
        private readonly IExpenseService _expenseService;
        private readonly IXlsService _xlsService;
        private readonly IDataSanitizerService _normalizeService;
        private readonly IFinancialIntelligenceService _processStatementService;

        //public string _statementFilePath;
        //public string _expenseFilePath;
        public string _finalFilePath;

        public List<Expense> _expenses;

        public BankType bank;

        public BankService(IExpenseService expenseService,
                         IXlsService xlsService,
                         IDataSanitizerService normalizeService,
                         IFinancialIntelligenceService processStatementService
                         )
        {
            _expenseService = expenseService;
            _xlsService = xlsService;
            _normalizeService = normalizeService;
            _processStatementService = processStatementService;
        }

        //public async Task<StatementResponse> ProcessRawBankDetailsAsync(string statementFilePath, string expenseFilePath, string finalFilePath)
        //{
        //    StatementResponse statementResponse = new StatementResponse();

        //    try
        //    {
        //        #region CARGA DOS DADOS BRUTOS

        //        _finalFilePath = finalFilePath;

        //        //01 CARREGO A LISTA DE DESPESAS CONHECIDAS
        //        _expenses = await _expenseService.GetAll();

        //        //02 CARREGO OS DADOS DO EXTRATO
        //        List<SpendingData> spendingDataList = new List<SpendingData>();

        //        string[] lines = File.ReadAllLines(statementFilePath, Encoding.Latin1);

        //        int cabecalho = 0;
        //        string? xlsName = null;

        //        foreach (string line in lines)
        //        {
        //            SpendingData spendingDataItem = new SpendingData();

        //            string cleanLine = line.Replace("\"", "");
        //            string[] aItem = cleanLine.Split(line.Contains(";") ? ';' : ',');

        //            if (cabecalho == 0)
        //            {
        //                //02.01 DETERMINA O BANCO & DEFINE CABEÇALHO PADRÃO

        //                //BANCO DO BRASIL
        //                //--0---------1-----------------2-------------3--------------------4----------------5----
        //                //Data";"Dependencia Origem";"Histórico";"Data do Balancete";"Número do documento";"Valor"

        //                //NUBANK
        //                //------0--1--------2---
        //                //date";"title";"amount"

        //                //string cleanCab = line.Replace("\"", "");
        //                //string[] aItem = cleanCab.Split(line.Contains(";") ? ';' : ',');

        //                bank = BankType.BB;
        //                if (aItem[0].StartsWith("DATE", StringComparison.OrdinalIgnoreCase))
        //                {
        //                    bank = BankType.Nubank;
        //                }

        //                spendingDataItem.Date = "DATA";
        //                spendingDataItem.Subject = "CASA";
        //                spendingDataItem.StringValue = "VALOR";
        //                spendingDataItem.Type = "TIPO";
        //                spendingDataItem.Score = "SCORE";
        //            }
        //            else
        //            {
        //                if (bank == BankType.BB)
        //                {
        //                    spendingDataItem.Date = aItem[0];
        //                    spendingDataItem.Subject = aItem[2].ToUpper();

        //                    spendingDataItem.GrossValue = aItem[5];
        //                    spendingDataItem.StringValue = _normalizeService.NormalizeValue(aItem[5]);
        //                    spendingDataItem.DecimalValue = _normalizeService.NormalizeToDecimal(spendingDataItem.StringValue);
        //                    spendingDataItem.IsCredit = !aItem[5].Contains("-");
        //                }
        //                else
        //                {
        //                    spendingDataItem.Date = NormalizeDate(aItem[0]);
        //                    spendingDataItem.Subject = aItem[1].ToUpper();

        //                    spendingDataItem.GrossValue = aItem[2];
        //                    spendingDataItem.StringValue = _normalizeService.NormalizeValue(aItem[2]);
        //                    spendingDataItem.DecimalValue = _normalizeService.NormalizeToDecimal(spendingDataItem.StringValue);
        //                    spendingDataItem.IsCredit = aItem[2].Contains("-");
        //                }

        //                spendingDataItem = _processStatementService.ProcessSubject(spendingDataItem, _expenses);

        //                //------------------------------------------------------

        //                if (string.IsNullOrEmpty(xlsName))
        //                {
        //                    xlsName = _xlsService.CreateXlsArchiveName(bank, spendingDataItem.Date, "xlsx");
        //                }
        //            }

        //            spendingDataList.Add(spendingDataItem);
        //            cabecalho++;
        //        }

        //        #endregion CARGA DOS DADOS BRUTOS

        //        #region PROCESSAMENTO DAS DESPESAS FIXAS E SOMADORES

        //        statementResponse = _processStatementService.ProcessTotalKnowSpending(spendingDataList);

        //        #endregion PROCESSAMENTO DAS DESPESAS FIXAS E SOMADORES

        //        #region CRIACAO DO XLS FINAL

        //        xlsName = xlsName ?? "";
        //        string xlsFilePath = Path.Combine(finalFilePath, xlsName);

        //        if (_xlsService.CreateNewFileXLS(xlsFilePath, spendingDataList))
        //        {
        //            statementResponse.FilePath = xlsFilePath;
        //            return statementResponse;
        //        }
        //        else
        //        {
        //            return statementResponse;
        //        }

        //        #endregion CRIACAO DO XLS FINAL
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}

        private static string? NormalizeDate(string dateString)
        {
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy" };

            return DateTime.TryParseExact(dateString,
                                           formats,
                                           CultureInfo.InvariantCulture,
                                           DateTimeStyles.None,
                                          out var date) ? date.ToString("dd/MM/yyyy") : null;
        }

        StatementResponse IBankService.ProcessRawBankDetailsAsync(string statementFilePath, string expenseFilePath, string finalFilePath)
        {
            throw new NotImplementedException();
        }
    }
}