using Server.Api.Domain.Service.ProcessStatementService.Enum;

namespace Server.Api.Domain.Service.ProcessStatementService.Model
{
    public class SpendingData
    {
        public string? Date { get; set; }
        public string? Subject { get; set; }
        public decimal Value { get; set; }

        //----------------------------------------
        public bool IsCredit { get; set; }

        public FinancialType FinancialType { get; set; }

        public string? Owner { get; set; }
        public string? Bank { get; set; }
        public string? Score { get; set; }

        // --- Metadados para Rastreabilidade e I.A. ---

        /// <summary>
        /// Identifica se a categoria (Owner) veio de regra local ou IA.
        /// </summary>
        public string? SourceRule { get; set; }

        /// <summary>
        /// Indica se este item foi processado/categorizado pela Inteligência Artificial.
        /// </summary>
        public bool ProcessedByIA { get; set; }

        /// <summary>
        /// Nível de confiança da IA na categorização (0.0 a 1.0 ou 0 a 100).
        /// </summary>
        public double? ConfidenceLevel { get; set; }

        /// <summary>
        /// Armazena o motivo ou a justificativa da IA para tal categoria, 
        /// útil para conferência do usuário.
        /// </summary>
        public string? IAExplanation { get; set; }
    }
}