using Microsoft.AspNetCore.Http;
using Server.Api.Domain.Service.ProcessStatementService.Model;

namespace Server.Api.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface
{
    public interface IStatementService
    {
        /// <summary>
        /// Processa uma lista de arquivos de extrato, identifica o banco de origem
        /// e retorna uma lista unificada de transações.
        /// </summary>
        /// <param name="files">Lista de arquivos enviados via multipart/form-data</param>
        /// <returns>Uma lista padronizada de objetos TransactionModel</returns>
        Task<List<TransactionModel>> ProcessCsvFilesAsync(List<IFormFile> files);
    }
}