# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copia as soluções
COPY ["Server.Solution/Server.Solution.sln", "Server.Solution/"]
COPY ["Shared.Solution/Shared.Solution.sln", "Shared.Solution/"]

# 2. Copia os arquivos .csproj respeitando a árvore de pastas
COPY ["Server.Solution/Server.Api/Server.Api.csproj", "Server.Solution/Server.Api/"]
COPY ["Server.Solution/Server.Domain/Server.Domain.csproj", "Server.Solution/Server.Domain/"]
COPY ["Shared.Solution/Core.Infrastructure/Core.Infrastructure.csproj", "Shared.Solution/Core.Infrastructure/"]
COPY ["Shared.Solution/Core.AI.Contracts/Core.AI.Contracts.csproj", "Shared.Solution/Core.AI.Contracts/"]
COPY ["Shared.Solution/Core.AI.Infrastructure/Core.AI.Infrastructure.csproj", "Shared.Solution/Core.AI.Infrastructure/"]

# 3. Restaura as dependências
RUN dotnet restore "Server.Solution/Server.Solution.sln"

# 4. Copia todo o código fonte e publica
COPY . .
WORKDIR "/src/Server.Solution/Server.Api"
RUN dotnet publish "Server.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5020
EXPOSE 5020
ENTRYPOINT ["dotnet", "Server.Api.dll"]