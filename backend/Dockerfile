FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar solution e projetos
COPY ZulexPharma.sln ./
COPY ZulexPharma.Domain/*.csproj ZulexPharma.Domain/
COPY ZulexPharma.Application/*.csproj ZulexPharma.Application/
COPY ZulexPharma.Infrastructure/*.csproj ZulexPharma.Infrastructure/
COPY ZulexPharma.API/*.csproj ZulexPharma.API/

# Restaurar pacotes
RUN dotnet restore

# Copiar tudo e publicar
COPY . .
RUN dotnet publish ZulexPharma.API/ZulexPharma.API.csproj -c Release -o /app/out

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
ENTRYPOINT ["dotnet", "ZulexPharma.API.dll"]
