using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Server.Web.Components.Shared;

public partial class UploadExpenseComponent : ComponentBase
{
    protected IReadOnlyList<IBrowserFile> files;

    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> OnUpload { get; set; }

    protected void HandleFileSelected(InputFileChangeEventArgs e)
    {
        files = e.GetMultipleFiles();
        StateHasChanged(); // Garante que o componente saiba que o estado mudou
    }

    protected async Task Upload()
    {
        if (files != null)
            await OnUpload.InvokeAsync(files);
    }
}