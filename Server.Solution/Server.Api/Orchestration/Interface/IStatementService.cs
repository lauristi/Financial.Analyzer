using Microsoft.AspNetCore.Http;
using Server.Api.Models;

namespace Server.Api.Orchestration.Interface
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