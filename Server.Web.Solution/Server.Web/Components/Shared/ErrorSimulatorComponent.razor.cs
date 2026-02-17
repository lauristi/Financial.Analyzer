using Microsoft.AspNetCore.Components;

namespace Server.Web.Components.Shared;

public partial class ErrorSimulatorComponent : ComponentBase
{
    [Inject]
    protected HttpClient Http { get; set; }

    protected string ResponseJson { get; set; } = string.Empty;
    protected string LastStatusCode { get; set; } = "---";

    protected async Task CallEndpoint(string endpoint)
    {
        ResponseJson = "Aguardando resposta do servidor...";

        try
        {
            // Realiza a chamada manual para capturar o HttpResponseMessage completo
            var response = await Http.GetAsync($"api/ErrorTest/{endpoint}");

            LastStatusCode = $"{(int)response.StatusCode} {response.StatusCode}";

            // Captura o corpo da resposta (JSON do Middleware)
            var content = await response.Content.ReadAsStringAsync();

            // Atribui o conteúdo bruto para exibição no componente
            ResponseJson = content;
        }
        catch (HttpRequestException ex)
        {
            ResponseJson = $"Erro de requisição: {ex.Message}";
            LastStatusCode = "Falha de Conexão";
        }
        catch (Exception ex)
        {
            ResponseJson = $"Erro inesperado: {ex.Message}";
            LastStatusCode = "Erro Interno";
        }
    }
}