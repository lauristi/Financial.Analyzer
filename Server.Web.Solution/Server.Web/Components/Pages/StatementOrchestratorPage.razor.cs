using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Server.Web.Service.Interface;
using Server.Web.Services.Interfaces;
using Server.Web.Services.Models.GroupedModel;

namespace Server.Web.Components.Pages;

public partial class StatementOrchestratorPage : ComponentBase
{
    [Inject] protected IFinancialService FinancialService { get; set; } = default!;
    [Inject] protected IFileService FileService { get; set; } = default!;
    [Inject] protected IAlertService AlertService { get; set; } = default!;

    protected StatementResult statementResult { get; set; } = new();

    private const long MaxFileSize = 20 * 1024 * 1024; // 20MB

    #region Gerenciamento do TAB

    private string activeTab = "main"; // "main" ou "debug"

    private void ChangeTab(string tabName)
    {
        activeTab = tabName;
    }

    #endregion

    #region Upload

    protected async Task ProcessStatementUpload(IReadOnlyList<IBrowserFile> files)
    {
        if (files == null || !files.Any()) return;

        try
        {
            // 1. Prepara o conteúdo para o envio
            var content = await CreateMultipartContent(files, "files");

            var result = await FinancialService.ProcessStatementAsync(content);

            // 3. Agora a variável 'result' existe e pode ser verificada
            if (result.IsSuccess && result.Value != null)
            {
                UpdateDashboard(result.Value);

                // Conversão do conteúdo para download
                var fileBytes = Convert.FromBase64String(result.Value.FileBase64);
                var fileName = System.IO.Path.GetFileName(result.Value.FilePath);

                await FileService.DownloadFileByteAsync(fileName, fileBytes);
                await AlertService.Show("Extrato processado com sucesso!", "alert-success");
            }
            else
            {
                // Caso o processamento retorne erros de negócio
                await AlertService.Show(result, "alert-danger");
            }
        }
        catch (Exception ex)
        {
            // Captura falhas de comunicação ou erros inesperados
            await AlertService.Show($"Erro crítico: {ex.Message}", "alert-danger");
        }
    }

    protected async Task ProcessExpenseUpload(IReadOnlyList<IBrowserFile> files)
    {
        if (files == null || !files.Any()) return;

        try
        {
            var content = await CreateMultipartContent(files, "file");
            var result = await FinancialService.UploadExpensesAsync(content);

            if (result.IsSuccess)
                await AlertService.Show("Despesas atualizadas!", "alert-success");
            else
                await AlertService.Show(result, result.IsSuccess ? "alert-success" : "alert-danger");
        }
        catch (Exception ex)
        {
            await AlertService.Show($"Erro no envio: {ex.Message}", "alert-danger");
        }
    }

    private async Task<MultipartFormDataContent> CreateMultipartContent(IReadOnlyList<IBrowserFile> files, string name)
    {
        var content = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var ms = new MemoryStream();
            await file.OpenReadStream(MaxFileSize).CopyToAsync(ms);
            ms.Position = 0;
            content.Add(new StreamContent(ms), name, file.Name);
        }
        return content;
    }

    #endregion Upload

    #region Dashboard

    private void UpdateDashboard(StatementResult data)
    {
        // Atualiza a variável que está vinculada ao componente visual no HTML
        this.statementResult = data;
    }

    #endregion Dashboard
}