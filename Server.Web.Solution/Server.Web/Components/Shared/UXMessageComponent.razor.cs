using Microsoft.AspNetCore.Components;
using Server.Web.Services.Interfaces;

namespace Server.Web.Components.Shared;

public partial class UXMessageComponent : ComponentBase, IDisposable
{
    [Inject]
    protected IAlertService AlertService { get; set; } = default!;

    protected override void OnInitialized()
    {
        // Subscreve ao evento utilizando um mediador que garante a atualização da UI
        AlertService.OnChange += NotifyStateChanged;
    }

    private async void NotifyStateChanged()
    {
        // O InvokeAsync é fundamental para garantir que a re-renderização 
        // ocorra na thread correta da interface.
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        AlertService.OnChange -= NotifyStateChanged;
    }
}