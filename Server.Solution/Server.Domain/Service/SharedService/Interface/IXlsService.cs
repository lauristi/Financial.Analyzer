using Server.Api.Domain.Service.ProcessStatementService.Enum;
using Server.Api.Domain.Service.ProcessStatementService.Model;

namespace Server.Api.Domain.Service.InfrastrutureService.Interface
{
    public interface IXlsService
    {
        string ConvertCsvToXls(string csvFilePath, string xlsFilePath);

        //bool CreateNewFileCSV(string finalFilePath, List<SpendingData> spendingData);

        bool CreateNewFileXLS(string xlsFilePath, List<SpendingData> spendingData);

        string CreateXlsArchiveName(BankType bankType, string dateString, string extension);
    }
}