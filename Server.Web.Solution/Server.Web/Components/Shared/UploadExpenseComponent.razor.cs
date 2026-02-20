using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Server.Web.Services.Interfaces;

namespace Server.Web.Components.Shared;

public partial class UploadExpenseComponent : ComponentBase
{
    [Inject]
    protected IAlertService AlertService { get; set; } = default!;

    protected IReadOnlyList<IBrowserFile> files;

    // O IsLoading permanece para controlar o estado do botão
    protected bool IsLoading { get; set; } = false;

    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> OnUpload { get; set; }

    protected void HandleFileSelected(InputFileChangeEventArgs e)
    {
        files = e.GetMultipleFiles();
        StateHasChanged();
    }

    protected async Task Upload()
    {
        if (files != null && !IsLoading)
        {
            try
            {
                IsLoading = true;

                // Utilizamos o padrão de mensagem informativa do seu AlertService
                // Note: Se você implementou o ShowLoading sem timer, use-o aqui.
                // Caso contrário, usamos o Show padrão com um tempo longo.
                await AlertService.Show("Processando lista de despesas... Por favor, aguarde.", "alert-info");

                // Notifica o componente pai
                await OnUpload.InvokeAsync(files);
            }
            catch (Exception)
            {
                await AlertService.Show("Erro ao processar a lista de despesas.", "alert-danger");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
    }
}