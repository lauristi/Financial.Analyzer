using Core.Infrastructure.Common;
using Server.Web.Services.Interfaces;
using Server.Web.Services.Models.GroupedModel;

public class AlertService : IAlertService
{
    public string Message { get; private set; } = string.Empty;
    public string CssClass { get; private set; } = "alert-info";
    public bool IsVisible { get; private set; }
    public event Action? OnChange;

    // Ajustado para receber o objeto OperationResult diretamente
    // Alteramos para aceitar qualquer tipo de OperationResult<T>
    public async Task Show<T>(OperationResult<T> result, string css)
    {
        if (result.Errors != null && result.Errors.Any())
        {
            Message = string.Join(Environment.NewLine, result.Errors);
        }
        else if (!result.IsSuccess)
        {
            Message = result.ErrorMessage ?? "Ocorreu um erro desconhecido.";
        }
        else
        {
            // Se for um sucesso e não houver mensagem específica, definimos um padrão
            Message = "Operação realizada com sucesso!";
        }

        CssClass = css;
        IsVisible = true;
        NotifyStateChanged();

        await Task.Delay(4000);

        IsVisible = false;
        NotifyStateChanged();
    }

    // Mantemos o overload para mensagens simples
    public async Task Show(string message, string css)
    {
        Message = message;
        CssClass = css;
        IsVisible = true;
        NotifyStateChanged();

        await Task.Delay(14000);

        IsVisible = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}