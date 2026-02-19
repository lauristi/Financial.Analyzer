using Core.AI.Contracts.Models;

namespace Core.AI.Contracts.Interfaces
{
    /// <summary>
    /// Define as capacidades do Analista Financeiro baseado em IA.
    /// Esta abstração permite que a infraestrutura seja trocada (ex: Ollama por Azure OpenAI)
    /// sem alterar a lógica de negócio na camada Core.
    /// </summary>
    public interface IFinancialAiAnalyst
    {
        /// <summary>
        /// Processa a descrição de uma única transação para extrair significado semântico.
        /// </summary>
        /// <param name="description">O texto bruto vindo do extrato bancário.</param>
        /// <param name="cancellationToken">Permite interromper a tarefa caso demore muito,
        /// o que é crítico ao rodar em hardware com limitações térmicas.</param>
        /// <returns>Um resultado de análise estruturado.</returns>
        Task<TransactionAnalysisResult> AnalyzeTransactionAsync(
            string description,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processa múltiplas transações em uma única sessão.
        /// O processamento em lote (batch) costuma ser mais eficiente para o LLM do que várias chamadas individuais.
        /// </summary>
        /// <param name="descriptions">Uma coleção de descrições de transações.</param>
        /// <param name="cancellationToken">Padrão recomendado para operações assíncronas.</param>
        /// <returns>Uma coleção de resultados de análise mapeados para as entradas.</returns>
        Task<IEnumerable<TransactionAnalysisResult>> AnalyzeTransactionBatchAsync(
            IEnumerable<string> descriptions,
            CancellationToken cancellationToken = default);
    }
}