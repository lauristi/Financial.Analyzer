public class ResponseEnvelope<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } // Mensagem universal (Sucesso ou Erro)
    public T? Value { get; set; }       // Dados opcionais (pode ser null)
    public string? ErrorCode { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    // Construtor vazio para serialização
    public ResponseEnvelope()
    { }

    // Fábrica para Sucesso (com ou sem dados)
    public static ResponseEnvelope<T> Success(string message, T? value = default)
        => new() { 
            IsSuccess = true, 
            Message = message, 
            Value = value 
        };

    // Fábrica para Falha
    public static ResponseEnvelope<T> Failure(string message, string? errorCode = null, IEnumerable<string>? errors = null)
        => new()
        {
            IsSuccess = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors
        };
}