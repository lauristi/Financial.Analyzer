using Microsoft.AspNetCore.Http;
using Server.Api.Domain.Service.StatmentOrchestration.Model.GroupedModel;

namespace Server.Api.Domain.Service.ProcessStatementService.Interface
{
    /// <summary>
    /// Interface maestra responsável por orquestrar o fluxo completo de 
    /// processamento de extratos bancários de múltiplas origens.
    /// </summary>
    public interface IStatementOrchestratorService
    {
        /// <summary>
        /// Executa o pipeline completo: Identificação, Extração, Mapeamento, 
        /// Classificação e Consolidação dos arquivos enviados.
        /// </summary>
        /// <param name="files">Lista de arquivos CSV enviados via API.</param>
        /// <returns>Objeto StatementResponse contendo os totais e a lista classificada para o Dashboard.</returns>
        Task<StatementResponse> ExecuteOrchestrationAsync(List<IFormFile> files);
    }
}