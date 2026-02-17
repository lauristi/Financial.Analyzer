using Core.Infrastructure.Common;
using Server.Web.Services.Models.GroupedModel;

namespace Server.Web.Services.Interfaces
{
    public interface IAlertService
    {
        // Contrato para os dados que a UI vai consumir
        string Message { get; }

        string CssClass { get; }
        bool IsVisible { get; }

        // Contrato para o evento de notificação
        event Action? OnChange;

        // Contrato para a ação de exibir o alerta
        Task Show<T>(OperationResult<T> result, string css);
        Task Show(string message, string css);
    }
}