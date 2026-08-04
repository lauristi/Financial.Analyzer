/// <summary>
/// Classe auxiliar interna para mapear o contrato JSON esperado da resposta da Inteligência Artificial.
/// </summary>
public class AiTransactionResult
{
    public string? SuggestedCategory { get; set; }
    public Double ConfidenceLevel { get; set; }
    public string? PointOfAttention { get; set; }
    public string? Reasoning { get; set; }
}