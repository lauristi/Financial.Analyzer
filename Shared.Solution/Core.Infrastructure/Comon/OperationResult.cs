namespace Core.Infrastructure.Common
{
    /// <summary>
    /// Objeto genérico para transportar o resultado de operações entre camadas.
    /// Centraliza o sucesso ou a falha (simples ou múltipla) sem o uso de exceções.
    /// </summary>
    /// <typeparam name="T">Tipo do valor retornado em caso de sucesso.</typeparam>
    public class OperationResult<T>
    {
        #region Propriedades

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T Value { get; }
        public string ErrorMessage { get; }
        public string ErrorCode { get; }
        public IEnumerable<string> Errors { get; }

        #endregion Propriedades

        #region Construtor

        /// <summary>
        /// Construtor privado para garantir a consistência através das fábricas estáticas.
        /// </summary>
        private OperationResult(bool isSuccess, T value, string errorMessage, string errorCode, IEnumerable<string> errors = null)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
            // Garante que a coleção de erros nunca seja nula para evitar NullReferenceException no futuro.
            Errors = errors ?? new List<string>();
        }

        #endregion Construtor

        #region Fabrica

        /// <summary>
        /// Cria um resultado de sucesso contendo o valor da operação.
        /// </summary>
        public static OperationResult<T> Success(T value)
            => new(true, value, null, null);

        /// <summary>
        /// Cria um resultado de falha única com uma mensagem e código de erro.
        /// </summary>
        public static OperationResult<T> Failure(string message, string errorCode = "GENERIC_ERROR")
            => new(false, default, message, errorCode);

        /// <summary>
        /// Cria um resultado de falha múltipla, ideal para validações de formulários.
        /// </summary>
        public static OperationResult<T> Failure(IEnumerable<string> errors, string errorCode = "VALIDATION_ERROR")
            => new(false, default, "Um ou mais erros de validação ocorreram.", errorCode, errors);

        #endregion Fabrica
    }
}