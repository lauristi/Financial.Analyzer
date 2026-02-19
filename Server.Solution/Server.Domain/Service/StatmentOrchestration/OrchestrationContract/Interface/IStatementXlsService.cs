using Server.Api.Domain.Service.ProcessStatementService.Model;

namespace Server.Api.Domain.Service.InfrastrutureService.Interface
{
    public interface IStatementXlsService
    {
        /// <summary>
        /// Gera um arquivo Excel formatado a partir dos dados processados.
        /// </summary>
        /// <param name="xlsFilePath">Caminho completo onde o arquivo será salvo.</param>
        /// <param name="statementResponse">Objeto contendo a lista de transações e o dashboard.</param>
        /// <returns>O caminho do arquivo gerado em caso de sucesso.</returns>
        string CreateStatementExcel(List<SpendingData> spendingDataList);
    }
}