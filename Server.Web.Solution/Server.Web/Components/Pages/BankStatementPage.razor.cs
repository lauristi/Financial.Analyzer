using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ServerBB_Web.Service.Interface;
using ServerBB_Web.Service.Model;
using System.Text.Json;

namespace ServerBB_Web.Components.Pages;

public partial class BankStatementPage : ComponentBase
{
    // Injeta HttpClient para comunicação com sua API backend
    [Inject] protected HttpClient HttpClient { get; set; } = default!;

    // Serviço JSInterop que você já usa para baixar arquivos no browser
    [Inject] protected IFileService FileService { get; set; } = default!;


    // ===== Dados exibidos no Dashboard =====
    // Esses valores serão preenchidos após o processamento do extrato

    protected decimal SuperMarket { get; set; }
    protected decimal Pharmacy { get; set; }
    protected decimal TotalDebit { get; set; }
    protected decimal TotalCredit { get; set; }


    // ===== Controle de mensagens na tela =====

    protected string UploadMessage { get; set; } = string.Empty; // Texto exibido ao usuário
    protected string AlertClass { get; set; } = "alert-info";    // Classe CSS do alerta
    protected bool ShowMessage { get; set; }                     // Controla visibilidade


    // Limite máximo do tamanho de arquivo permitido
    // Blazor exige esse controle por segurança
    private const long MaxFileSize = 20 * 1024 * 1024; // 20MB



    // ==========================================================
    // PROCESSAMENTO DO EXTRATO BANCÁRIO
    // ==========================================================
    protected async Task ProcessStatementUpload(IReadOnlyList<IBrowserFile> files)
    {
        // Proteção contra chamada sem arquivos
        if (files == null || files.Count == 0)
            return;

        try
        {
            // Multipart é necessário para enviar arquivos via HTTP
            var content = new MultipartFormDataContent();

            // Permite upload de múltiplos arquivos
            foreach (var file in files)
            {
                // Copiamos o arquivo do browser para memória
                using var ms = new MemoryStream();

                await file.OpenReadStream(MaxFileSize).CopyToAsync(ms);

                // Reseta posição do stream para leitura
                ms.Seek(0, SeekOrigin.Begin);

                // Adiciona o arquivo ao request HTTP
                content.Add(
                    new StreamContent(new MemoryStream(ms.ToArray())),
                    "files",
                    file.Name);
            }

            // Envia os arquivos para API
            var responseUpload =
                await HttpClient.PostAsync("api/bb/uploadStatement", content);

            // Se falhar upload, interrompe fluxo
            if (!responseUpload.IsSuccessStatusCode)
            {
                await SetUploadMessageAsync("Erro upload extrato", "alert-danger");
                return;
            }

            // Após upload, chama endpoint que processa e devolve resultado
            var responseDownload =
                await HttpClient.GetAsync("api/bb/multiPartProcessFile");

            if (!responseDownload.IsSuccessStatusCode)
            {
                await SetUploadMessageAsync("Erro processamento extrato", "alert-danger");
                return;
            }

            // Recebe JSON com resultados do processamento
            var json = await responseDownload.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<MultiPartResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null)
            {
                await SetUploadMessageAsync("Resposta inválida do servidor", "alert-danger");
                return;
            }

            // Atualiza valores exibidos no dashboard
            SuperMarket = result.SuperMarket;
            Pharmacy = result.Pharmacy;
            TotalDebit = result.TotalDebit;
            TotalCredit = result.TotalCredit;


            // Baixa o XLS gerado pela API
            await FileService.DownloadFileByteAsync(
                result.FileName,
                result.FileContent);

            await SetUploadMessageAsync("Extrato processado", "alert-success");
        }
        catch (Exception)
        {
            await SetUploadMessageAsync("Erro inesperado no processamento", "alert-danger");
        }
    }



    // ==========================================================
    // PROCESSAMENTO DO CSV DE DESPESAS FIXAS
    // ==========================================================
    protected async Task ProcessExpenseUpload(IReadOnlyList<IBrowserFile> files)
    {
        if (files == null || files.Count == 0)
            return;

        try
        {
            // Aqui você aceita apenas 1 arquivo
            var file = files.First();

            using var ms = new MemoryStream();

            await file.OpenReadStream(MaxFileSize).CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var content = new MultipartFormDataContent();

            content.Add(
                new StreamContent(new MemoryStream(ms.ToArray())),
                "file",
                file.Name);

            var response =
                await HttpClient.PostAsync("api/bb/uploadExpenses", content);

            if (response.IsSuccessStatusCode)
                await SetUploadMessageAsync("Despesas enviadas", "alert-success");
            else
                await SetUploadMessageAsync("Erro envio despesas", "alert-danger");
        }
        catch (Exception)
        {
            await SetUploadMessageAsync("Erro inesperado no envio", "alert-danger");
        }
    }



    // ==========================================================
    // MÉTODO RESPONSÁVEL POR MOSTRAR ALERTAS NA UI
    // ==========================================================
    private async Task SetUploadMessageAsync(string message, string css)
    {
        UploadMessage = message;
        AlertClass = css;
        ShowMessage = true;

        // Força atualização da tela
        StateHasChanged();

        // Mantém mensagem visível por 3 segundos
        await Task.Delay(3000);

        ShowMessage = false;

        // Atualiza novamente a UI
        StateHasChanged();
    }
}
