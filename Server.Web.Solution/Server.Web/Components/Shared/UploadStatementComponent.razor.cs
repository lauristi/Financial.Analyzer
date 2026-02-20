using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Server.Web.Services.Interfaces;

namespace Server.Web.Components.Shared;

public partial class UploadStatementComponent : ComponentBase
{
    [Inject]
    protected IAlertService AlertService { get; set; } = default!;

    protected IReadOnlyList<IBrowserFile> files;
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

                // Usamos o novo método sem timer para o carregamento
                AlertService.ShowLoading("Consultando Inteligência Artificial... Por favor, aguarde.");

                // O InvokeAsync chama o orquestrador que retorna o StatementResponse
                // Supondo que o retorno do OnUpload seja capturado no componente pai
                await OnUpload.InvokeAsync(files);

                // O componente pai provavelmente chamará o AlertService.Show(result, "alert-success")
                // após o retorno da API, o que naturalmente sobrescreverá o alerta de loading.
            }
            catch (Exception)
            {
                await AlertService.Show("Ocorreu um erro inesperado ao enviar os arquivos.", "alert-danger");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
    }

}