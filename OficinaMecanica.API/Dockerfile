# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os .csproj e restaura dependências
COPY OficinaMecanica.Domain/OficinaMecanica.Domain.csproj OficinaMecanica.Domain/
COPY OficinaMecanica.Application/OficinaMecanica.Application.csproj OficinaMecanica.Application/
COPY OficinaMecanica.Infrastructure/OficinaMecanica.Infrastructure.csproj OficinaMecanica.Infrastructure/
COPY OficinaMecanica.API/OficinaMecanica.API.csproj OficinaMecanica.API/
RUN dotnet restore OficinaMecanica.API/OficinaMecanica.API.csproj

# Copia todo o código e publica
COPY . .
RUN dotnet publish OficinaMecanica.API/OficinaMecanica.API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OficinaMecanica.API.dll"]
