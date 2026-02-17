using Blazored.LocalStorage;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using NLog.Extensions.Logging;
using Server.Web.Components;
using Server.Web.Service.Interface;
using Server.Web.Services.Implementations;
using Server.Web.Services.Interfaces;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

#region 1. Configurações de Infraestrutura (Logging e Kestrel)

builder.Logging.AddNLog();

var configuration = builder.Configuration;

// Recuperação de valores do AppSettings
var apiBaseAddress = configuration["ConnectionSettings:ApiBaseAddress"]
    ?? throw new InvalidOperationException("ApiBaseAddress não configurado.");

var bindPort = int.Parse(configuration["ConnectionSettings:BindPort"] ?? "5023");

// Configuração do servidor Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(bindPort);
});

// Suporte para Web Assets estáticos em ambiente de produção
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseStaticWebAssets();
}

#endregion 1. Configurações de Infraestrutura (Logging e Kestrel)

#region 2. Injeção de Dependências (Services)

// Componentes do Razor e Interactive Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configuração de limites para SignalR (upload de arquivos)
builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 20 * 1024 * 1024; // 20MB
});

// Registro do HttpClient para comunicação com a API Backend
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });

// Registro dos Serviços de Aplicação (Padronizados com Interface)
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<IAlertService, AlertService>();

// Bibliotecas de terceiros
builder.Services.AddBlazoredLocalStorage();

#endregion 2. Injeção de Dependências (Services)

var app = builder.Build();

#region 3. Pipeline de Requisições HTTP (Middlewares)

// Configuração de Localização (pt-BR)
var supportedCultures = new[] { new CultureInfo("pt-BR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Ajuste global de cultura para Threads
CultureInfo.DefaultThreadCurrentCulture = supportedCultures[0];
CultureInfo.DefaultThreadCurrentUICulture = supportedCultures[0];

// Tratamento de Erros e Segurança em Produção
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Headers de Proxy (necessário para Nginx/Docker)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Mapeamento dos Componentes Blazor
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

#endregion 3. Pipeline de Requisições HTTP (Middlewares)

app.Run();