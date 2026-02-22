# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia a Solution e os projetos usando os caminhos da sua imagem
COPY ["Financial.Analyzer/Financial.Analyzer.sln", "Financial.Analyzer/"]
COPY ["Server.Solution/Server.Api/Server.Api.csproj", "Server.Solution/Server.Api/"]
COPY ["Shared.Solution/Core.Infrastructure/Core.Infrastructure.csproj", "Shared.Solution/Core.Infrastructure/"]
COPY ["Api/Server.Domain/Server.Domain.csproj", "Api/Server.Domain/"]

# Restaura as dependências
RUN dotnet restore "Financial.Analyzer/Financial.Analyzer.sln"

# Copia todo o conteúdo do repositório
COPY . .

# Muda para a pasta da API para publicar
WORKDIR "/src/Server.Solution/Server.Api"
RUN dotnet publish "Server.Api.csproj" -c Release -o /app/publish

# Estágio de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Configuração de porta e execução
ENV ASPNETCORE_URLS=http://+:5020
EXPOSE 5020
ENTRYPOINT ["dotnet", "Server.Api.dll"]