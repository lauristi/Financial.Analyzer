namespace Core.HttpHandleResults.Responses
{
    /// <summary>
    /// Modelo padrão para respostas de erro da API.
    /// Este é o JSON que o Front-end receberá em caso de falha.
    /// </summary>
    public class GenericApiErrorResponse
    {
        public bool Success { get; } = false; // Sempre falso neste objeto
        public string Title { get; set; }     // Ex: "Erro de Regra de Negócio" ou "Erro Interno"
        public string Message { get; set; }   // Mensagem amigável para o usuário
        public string ErrorCode { get; set; } // Código estável para o Front-end (ex: VAL_001)
        public string TraceId { get; set; }    // Para rastreamento em logs (opcional, mas profissional)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Lista para quando houver múltiplos erros (ex: validação de vários campos)
        public List<string> Errors { get; set; } = new List<string>();

        // Detalhes técnicos (preenchido apenas em desenvolvimento pelo Middleware)
        public string TechnicalDetail { get; set; }
    }
}