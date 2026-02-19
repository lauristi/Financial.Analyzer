namespace Core.AI.Contracts.Models
{
    /// <summary>
    /// Representa o resultado estruturado de uma análise financeira feita por IA.
    /// O uso de um DTO garante que o restante da aplicação não dependa do
    /// formato de texto bruto (string) retornado pelo modelo de linguagem.
    /// </summary>
    public class TransactionAnalysisResult
    {
        /// <summary>
        /// A categoria identificada pela IA (ex: 'Transporte', 'Alimentação', 'Lazer').
        /// </summary>
        public string SuggestedCategory { get; set; } = string.Empty;

        /// <summary>
        /// Um valor de 0.0 a 1.0 que indica o quão confiante a IA está nesta classificação.
        /// Útil para sinalizar resultados de baixa confiança que exijam revisão manual.
        /// </summary>
        public double ConfidenceLevel { get; set; }

        /// <summary>
        /// Uma breve explicação do porquê a IA escolheu esta categoria.
        /// Auxilia o desenvolvedor e o usuário a entenderem a lógica do modelo.
        /// </summary>
        public string Reasoning { get; set; } = string.Empty;
    }
}