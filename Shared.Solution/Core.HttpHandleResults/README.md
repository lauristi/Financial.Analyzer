# Documentação Técnica: Core.HttpHandleResults

Este projeto centraliza a base arquitetural para tratamento de fluxos, erros e respostas padronizadas da solução. O objetivo principal é garantir previsibilidade para o Front-end e robustez para o Back-end.

---

## 1. O Padrão de Resposta (Result Pattern)

Em vez de utilizarmos exceções para controlar regras de negócio (o que é custoso e desorganizado), utilizamos a classe genérica `OperationResult<T>`.

### Estrutura da Classe: `OperationResult<T>`
Localizada em: `Core.HttpHandleResults.Common`

- **IsSuccess**: Booleano que indica o estado da operação.
- **Value**: Objeto de retorno (apenas em caso de sucesso).
- **ErrorCode**: Código estável para lógica do Front-end (ex: `AUTH_001`).
- **Errors**: Lista de strings para acumular múltiplas falhas de validação.

---

## 2. Guia de Implementação por Cenário

### A. Sucesso Simples
Quando a operação ocorre conforme o esperado.
```csharp
public OperationResult<int> CalcularIdade(DateTime nascimento)
{
    var idade = DateTime.Now.Year - nascimento.Year;
    return OperationResult<int>.Success(idade);
}


B. Falha de Negócio Única
Quando uma regra impede a execução, mas o sistema está funcionando corretamente.

C#
public OperationResult<bool> ValidarSaldo(decimal valor)
{
    if (valor > _saldoAtual)
        return OperationResult<bool>.Failure("Saldo insuficiente para a transação.", "FIN_002");

    return OperationResult<bool>.Success(true);
}
C. Validações Múltiplas (Cadastro/Formulários)
Ideal para retornar todos os erros de uma vez ao usuário.

C#
public OperationResult<Usuario> CriarUsuario(Usuario user)
{
    var falhas = new List<string>();

    if (string.IsNullOrEmpty(user.Email)) falhas.Add("O e-mail é obrigatório.");
    if (user.Senha.Length < 8) falhas.Add("A senha deve ter no mínimo 8 caracteres.");

    if (falhas.Any())
        return OperationResult<Usuario>.Failure(falhas, "VALIDATION_USER_01");

    return OperationResult<Usuario>.Success(user);
}
3. A Rede de Segurança: GlobalExceptionMiddleware
O Middleware é o componente de infraestrutura que intercepta erros imprevistos (exceções não tratadas).

Características:
Centralização: Elimina a necessidade de try-catch genéricos nos Controllers.

Segurança de Dados: Em ambientes de Produção, o detalhe técnico do erro (StackTrace) é ocultado.

Padronização: Garante que o Front-end receba sempre o objeto ApiErrorResponse, mesmo em falhas críticas.

4. O Envelope de Saída: ApiErrorResponse
Este é o contrato final enviado via HTTP. O Front-end deve estar preparado para ler esta estrutura sempre que o Status Code não for 2xx.

JSON
{
  "Success": false,
  "Title": "Erro na Operação",
  "Message": "Mensagem amigável ao usuário",
  "ErrorCode": "CODIGO_ESTAVEL",
  "TraceId": "ID_UNICO_PARA_LOGS",
  "Timestamp": "2026-02-12T15:00:00Z",
  "Errors": ["Erro 1", "Erro 2"],
  "TechnicalDetail": "Detalhes técnicos (apenas em Desenvolvimento)"
}
5. Como configurar no Projeto Web (API)
Adicione a referência do projeto Core.HttpHandleResults.

No arquivo Program.cs, registre o middleware logo após o builder.Build():

C#
var app = builder.Build();

// Ativa a proteção global contra erros
app.UseMiddleware<GlobalExceptionMiddleware>();

app.Run();
Documento gerado para fins de estudo e padronização da Solution.