using Core.Ai.Agent.Services;
using Core.Ai.Agent.Services.Interfaces;
using Core.HttpHandleResults.Middlewares;
using Core.IA.Agente.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.OpenApi;
using OfficeOpenXml;
using Server.Api.Infrastructure;
using Server.Api.Infrastructure.Interface;
using Server.Api.Orchestration;
using Server.Api.Orchestration.Contracts;
using Server.Api.Orchestration.Interface;
using Server.Api.Parsers;
using Server.Api.Services;
using Server.Api.Services.Interfaces;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Registro de provedor para suporte a codificação Latin1 (ISO-8859-1)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);


// Define a licença não comercial no EPPlus 8+
ExcelPackage.License.SetNonCommercialPersonal("Hal");


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

#region 02. Parsers de Extratos Bancários

builder.Services.AddScoped<IBankParser, BbBankParser>();
builder.Services.AddScoped<IBankParser, NubankParser>();
builder.Services.AddScoped<BankParserFactory>();

#endregion 02. Parsers de Extratos Bancários

#region 03. Serviços do Domínio

builder.Services.AddScoped<IDataSanitizerService, DataSanitizerService>();
builder.Services.AddScoped<IExpenseService>(sp => new ExpenseService(appRootPath));
builder.Services.AddScoped<IStatementService, StatementService>();
builder.Services.AddScoped<IFinancialIntelligenceService, FinancialIntelligenceService>();
builder.Services.AddScoped<IStatementXlsService>(sp => new StatementXlsService(appRootPath));
builder.Services.AddScoped<IFinancialOrchestrator, FinancialOrchestrator>();

#endregion 03. Serviços do Domínio

#region 04. Injeção de Dependência para IA (Analista Financeiro)

// 1. Garante a leitura do arquivo externo de credenciais
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// 2. Registra as configurações de IA centralizadas na Class Library
builder.Services.AddAiAgentConfiguration(builder.Configuration);

// 3. Registra o serviço de IA no container do .NET
builder.Services.AddScoped<IAiCoreAgentService, AiCoreAgentService>();

#endregion 04. Injeção de Dependência para IA (Analista Financeiro)

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