using Microsoft.AspNetCore.Http;
using Server.Api.Models;

namespace Server.Api.Orchestration.Interface
{
    public interface IStatementXlsService
    {
        /// <summary>
        /// Gera um arquivo Excel formatado a partir dos dados processados.
        /// </summary>
        /// <param name="xlsFilePath">Caminho completo onde o arquivo será salvo.</param>
        /// <param name="statementResponse">Objeto contendo a lista de transações e o dashboard.</param>
        /// <returns>O caminho do arquivo gerado em caso de sucesso.</returns>
        Task<StatementResponse> CreatePreFormatedExcelAsync(StatementResponse statementResponse);

        Task<StatementResponse> CreateFinalExcelAsync(IFormFile file);
    }
}