using Microsoft.JSInterop;
using Server.Web.Service.Interface;

public class FileService : IFileService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public FileService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task DownloadFileByteAsync(string fileName, byte[] fileBytes)
    {
        // Carrega o módulo JavaScript isolado (Lazy Loading)
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./javascript/downloadModule.js");

        string base64String = Convert.ToBase64String(fileBytes);

        // Invoca a função exportada do arquivo .js
        await _module.InvokeVoidAsync("downloadFileFromBytes", fileName, base64String);
    }

    // Implementação do descarte assíncrono para liberar o módulo JS da memória
    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}