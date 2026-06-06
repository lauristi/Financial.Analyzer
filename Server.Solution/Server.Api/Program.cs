using Core.Ai.Agent.Services;
using Core.Ai.Agent.Services.Interfaces;
using Core.HttpHandleResults.Middlewares;
using Core.IA.Agente.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.OpenApi;
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
using Server.Domain.Service.StatmentOrchestration.OrchestrationContract;
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
builder.Services.AddScoped<IStatementService, StatementService>();
builder.Services.AddScoped<IStatementMapperService, StatementMapperService>();
builder.Services.AddScoped<IStatementOrchestratorService, StatementOrchestratorService>();

#endregion 02. Injeção de Dependência e AutoMapper

#region 03. Injeção de dependencia com Factory

// Registro base: essencial para que o IntelligenceService e o XlsService funcionem
builder.Services.AddScoped<IFinancialDashboardService, FinancialDashboarService>();

// Registro do Serviço de Excel utilizando uma "Factory" (Fábrica) customizada.
builder.Services.AddScoped<IStatementXlsService>(sp =>
{
    var dashboardService = sp.GetRequiredService<IFinancialDashboardService>();
    return new StatementXlsService(appRootPath, dashboardService);
});

#endregion 03. Injeção de dependencia com Factory

#region 04. Injecao de Dependência para Analista Financeiro de IA

// 1. Garante a leitura do seu arquivo externo de credenciais locais e de nuvem
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// 2. Registra as configurações de IA centralizadas na Class Library (Core.IA.Agente)
builder.Services.AddAiAgentConfiguration(builder.Configuration);

// 3. Registra o serviço de IA no container do .NET
builder.Services.AddScoped<IAiCoreAgentService, AiCoreAgentService>();

#endregion 04. Injecao de Dependência para Analista Financeiro de IA

#region 05. Configurações de Controladores e JSON

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

#endregion 05. Configurações de Controladores e JSON

#region 06. Swagger / OpenAPI Configuration

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Financial Analyzer API",
        Version = "v1",
        Description = "API para processamento de extratos bancários e análise financeira."
    });

    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

#endregion 06. Swagger / OpenAPI Configuration

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

#region 07. Pipeline de Middlewares (Configuração do App)

var supportedCultures = new[] { new CultureInfo("pt-BR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");

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

app.MapGet("/", () => Results.Redirect("/swagger/index.html"))
                             .ExcludeFromDescription();

#endregion 07. Pipeline de Middlewares (Configuração do App)

app.Run();