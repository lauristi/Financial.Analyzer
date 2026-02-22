# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copia todos os arquivos .sln e .csproj mantendo a estrutura de pastas
# Isso garante que o 'dotnet restore' encontre todos os projetos listados na Solution
COPY ["Server.Solution/Server.Solution.sln", "Server.Solution/"]
COPY ["Server.Solution/Server.Api/*.csproj", "Server.Solution/Server.Api/"]
COPY ["Server.Solution/Server.Domain/*.csproj", "Server.Solution/Server.Domain/"]
COPY ["Server.Solution/Server.Tests/*.csproj", "Server.Solution/Server.Tests/"]

# Ajuste para a pasta Shared conforme detectado anteriormente
COPY ["Shared/*.csproj", "Shared/"]

# 2. Restaura as dependências
# Agora ele encontrará os projetos de Testes e Infrastructure
RUN dotnet restore "Server.Solution/Server.Solution.sln"

# 3. Copia todo o restante do código fonte
COPY . .

# 4. Publica a API
WORKDIR "/src/Server.Solution/Server.Api"
RUN dotnet publish "Server.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5020
EXPOSE 5020
ENTRYPOINT ["dotnet", "Server.Api.dll"]