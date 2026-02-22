# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia a solução e todos os projetos para preservar a estrutura de dependências
COPY ["Financial.Analyzer.sln", "./"]
COPY ["Api/Server.Api/Server.Api.csproj", "Api/Server.Api/"]
COPY ["Shared/Core.Infrastructure/Core.Infrastructure.csproj", "Shared/Core.Infrastructure/"]
COPY ["Api/Server.Domain/Server.Domain.csproj", "Api/Server.Domain/"]
# Adicione aqui os demais projetos Core.AI, etc. que aparecem no seu print

RUN dotnet restore "Financial.Analyzer.sln"
COPY . .
WORKDIR "/src/Api/Server.Api"
RUN dotnet publish "Server.Api.csproj" -c Release -o /app/publish

# Estágio de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5020
EXPOSE 5020
ENTRYPOINT ["dotnet", "Server.Api.dll"]