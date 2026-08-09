# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copia as soluções (.sln)
COPY ["Server.Solution/Server.Solution.sln", "Server.Solution/"]
COPY ["Shared.Solution/Shared.Solution.sln", "Shared.Solution/"]

# 2. Copia todos os arquivos .csproj preservando a estrutura de pastas
COPY ["Server.Solution/Server.Api/Server.Api.csproj", "Server.Solution/Server.Api/"]
COPY ["Shared.Solution/Core.HttpHandleResults/Core.HttpHandleResults.csproj", "Shared.Solution/Core.HttpHandleResults/"]
COPY ["Shared.Solution/Core.Ai.Agent/Core.Ai.Agent.csproj", "Shared.Solution/Core.Ai.Agent/"]

# 3. Restaura as dependências
RUN dotnet restore "Server.Solution/Server.Solution.sln"

# 4. Copia o restante do código fonte e publica
COPY . .
WORKDIR "/src/Server.Solution/Server.Api"
RUN dotnet publish "Server.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Garante a existência do diretório Archives esperado pelo C#
RUN mkdir -p /app/Archives

ENV ASPNETCORE_URLS=http://+:5020
EXPOSE 5020
ENTRYPOINT ["dotnet", "Server.Api.dll"]