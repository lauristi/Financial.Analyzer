# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copia os arquivos de solução
COPY ["Server.Solution/Server.Solution.sln", "Server.Solution/"]

# 2. Copia os arquivos .csproj baseados na sua listagem real
COPY ["Server.Solution/Server.Api/Server.Api.csproj", "Server.Solution/Server.Api/"]
COPY ["Server.Solution/Server.Domain/Server.Domain.csproj", "Server.Solution/Server.Domain/"]
# Nota: O projeto Shared na sua lista aparece apenas como "Shared.", ajustado para a pasta correspondente
COPY ["Shared./", "Shared./"] 

# 3. Restaura as dependências usando a solução principal
RUN dotnet restore "Server.Solution/Server.Solution.sln"

# 4. Copia todo o conteúdo do repositório
COPY . .

# 5. Define o diretório de trabalho para a pasta da API
WORKDIR "/src/Server.Solution/Server.Api"

# 6. Publica o projeto
RUN dotnet publish "Server.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Configuração de porta e execução
ENV ASPNETCORE_URLS=http://+:5020
EXPOSE 5020
ENTRYPOINT ["dotnet", "Server.Api.dll"]