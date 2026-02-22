# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copia os arquivos de solução
COPY ["Server.Solution/Server.Solution.sln", "Server.Solution/"]

# 2. Copia os arquivos .csproj
COPY ["Server.Solution/Server.Api/Server.Api.csproj", "Server.Solution/Server.Api/"]
COPY ["Server.Solution/Server.Domain/Server.Domain.csproj", "Server.Solution/Server.Domain/"]

# --- AJUSTE AQUI ---
# Se a pasta no seu repositório for "Shared", remova o ponto. 
# Se houver dúvida, podemos usar um caractere curinga:
COPY ["Shared*", "Shared/"] 
# -------------------

# 3. Restaura as dependências
RUN dotnet restore "Server.Solution/Server.Solution.sln"

# 4. Copia todo o conteúdo
COPY . .

# 5. Publica
WORKDIR "/src/Server.Solution/Server.Api"
RUN dotnet publish "Server.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5020
EXPOSE 5020
ENTRYPOINT ["dotnet", "Server.Api.dll"]