FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar solution e projetos
COPY ZulexPharma.sln ./
COPY ZulexPharma.Domain/*.csproj ZulexPharma.Domain/
COPY ZulexPharma.Application/*.csproj ZulexPharma.Application/
COPY ZulexPharma.Infrastructure/*.csproj ZulexPharma.Infrastructure/
COPY ZulexPharma.API/*.csproj ZulexPharma.API/

# Restaurar pacotes — SO' da API (puxa Domain/Application/Infrastructure por dependencia).
# NAO usar `dotnet restore` sem alvo: ele restaura a solution inteira, que referencia
# ZulexPharma.Tests — projeto que a imagem de producao NAO copia (nem precisa).
RUN dotnet restore ZulexPharma.API/ZulexPharma.API.csproj

# Copiar tudo e publicar
COPY . .
RUN dotnet publish ZulexPharma.API/ZulexPharma.API.csproj -c Release -o /app/out

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["sh", "-c", "dotnet ZulexPharma.API.dll --urls http://0.0.0.0:${PORT:-8080}"]
