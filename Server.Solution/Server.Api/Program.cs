using Core.AI.Contracts.Interfaces;
using Core.AI.Infrastructure.Services;
using Core.Infrastructure.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Server.Api.Domain.Infrastructure.EncryptionLib;
using Server.Api.Domain.Service.BankService;
using Server.Api.Domain.Service.BankService.Interface;
using Server.Api.Domain.Service.ExpenseService;
using Server.Api.Domain.Service.InfrastrutureService;
using Server.Api.Domain.Service.InfrastrutureService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.Interface;
using Server.Api.Domain.Service.ProcessStatementService.OrchestrationContract;
using Server.Api.Domain.Service.ProcessStatementService.OrchestrationContract.Interface;
using Server.Api.Domain.Service.SharedService.Interface;
using Server.Api.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;
using Server.Domain.Service.StatmentOrchestration.OrchestrationContract.Interface;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Registro de provedor para suporte a codificação Latin1 (comum em arquivos bancários)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

#region 01. Configurações de Infraestrutura e Host (Kestrel/Logs)

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var configuration = builder.Configuration;
var appRootPath = builder.Environment.ContentRootPath;

var apiBaseAddress = configuration["ConnectionSettings:ApiBaseAddress"]
    ?? throw new InvalidOperationException("ConnectionSettings:ApiBaseAddress não configurado");

var bindPort = int.Parse(configuration["ConnectionSettings:BindPort"] ?? "5020");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(bindPort);
});

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseStaticWebAssets();
}

#endregion 01. Configurações de Infraestrutura e Host (Kestrel/Logs)

#region 02. Injeção de Dependência e AutoMapper

builder.Services.AddSingleton<ICrypto, Crypto>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// --- Serviços de Domínio ---
builder.Services.AddScoped<IDataSanitizerService, DataSanitizerService>();
builder.Services.AddScoped<IXlsService, XlsService>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<IFinancialIntelligenceService, FinancialIntelligenceService>();

builder.Services.AddScoped<IExpenseService>(sp => new ExpenseService(appRootPath));


// Registro do Serviço de Excel utilizando uma "Factory" (Fábrica) customizada.
// Usamos este formato porque o StatementXlsService possui um construtor misto:
builder.Services.AddScoped<IStatementXlsService>(sp =>
{
    //01  Resolve o serviço de Dashboard que já está no container
    //02  Retorna a instância da classe passando o parâmetro manual e o serviço resolvido
    var dashboardService = sp.GetRequiredService<IFinancialDashboardService>();

    return new StatementXlsService(appRootPath, dashboardService);
});

builder.Services.AddScoped<IStatementService, StatementService>();
builder.Services.AddScoped<IStatementMapperService, StatementMapperService>();
builder.Services.AddScoped<IStatementOrchestratorService, StatementOrchestratorService>();

#endregion 02. Injeção de Dependência e AutoMapper

#region Injecao de Dependência para Analista Financeiro de IA   

// Adiciona o arquivo de segredos à configuração
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// 1. Recuperamos as configurações do appsettings.json
var aiProvider = builder.Configuration["AiSettings:Provider"]; // "Ollama" ou "Gemini"

if (aiProvider == "Gemini")
{
    var apiKey = builder.Configuration["AiSettings:GeminiApiKey"];
    builder.Services.AddScoped<IFinancialAiAnalyst>(sp =>
        new GeminiFinancialAnalyst(apiKey));
}
else
{
    // Configuração padrão para o Ollama Local
    var dockerUri = new Uri(builder.Configuration["AiSettings:DockerUri"] ?? "npipe://./pipe/docker_engine");
    var ollamaUri = new Uri(builder.Configuration["AiSettings:OllamaUri"] ?? "http://localhost:11434");

    builder.Services.AddScoped<IFinancialAiAnalyst>(sp =>
        new OllamaFinancialAnalyst(dockerUri, ollamaUri));
}

// 2. Registramos o serviço de domínio que orquestra a inteligência
builder.Services.AddScoped<IFinancialIntelligenceService, FinancialIntelligenceService>();

#endregion

#region 03. Configurações de Controladores e JSON

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // PascalCase para compatibilidade com DTOs do Blazor
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

#endregion 03. Configurações de Controladores e JSON

#region 04. Swagger / OpenAPI Configuration

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Usamos o nome completo do tipo para evitar ambiguidades com outros pacotes
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Financial Analyzer API",
        Version = "v1",
        Description = "API para processamento de extratos bancários e análise financeira."
    });

    // Esta linha é um "truque" para garantir que o Swagger não se perca com
    // tipos complexos ou mapeamentos que você atualizou no NuGet
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

#endregion 04. Swagger / OpenAPI Configuration

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

#region 05. Pipeline de Middlewares (Configuração do App)

// Localização pt-BR
var supportedCultures = new[] { new CultureInfo("pt-BR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");

// Pipeline de Documentação
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Analyzer V1");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();
app.UseAuthentication();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.MapControllers();

// Redireciona a raiz para o Swagger (Exclude oculta da interface grafica)
app.MapGet("/", () => Results.Redirect("/swagger/index.html"))
                             .ExcludeFromDescription();

#endregion 05. Pipeline de Middlewares (Configuração do App)

app.Run();