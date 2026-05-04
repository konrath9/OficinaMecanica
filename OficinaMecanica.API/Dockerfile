# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os .csproj e restaura dependências
COPY OficinaMecanica.Domain/OficinaMecanica.Domain.csproj OficinaMecanica.Domain/
COPY OficinaMecanica.Application/OficinaMecanica.Application.csproj OficinaMecanica.Application/
COPY OficinaMecanica.Infrastructure/OficinaMecanica.Infrastructure.csproj OficinaMecanica.Infrastructure/
COPY OficinaMecanica.API/OficinaMecanica.API.csproj OficinaMecanica.API/
RUN dotnet restore OficinaMecanica.API/OficinaMecanica.API.csproj

# Copia apenas o codigo-fonte de cada projeto (obj/ e bin/ estao no .dockerignore)
COPY OficinaMecanica.Domain/ OficinaMecanica.Domain/
COPY OficinaMecanica.Application/ OficinaMecanica.Application/
COPY OficinaMecanica.Infrastructure/ OficinaMecanica.Infrastructure/
COPY OficinaMecanica.API/ OficinaMecanica.API/
COPY nuget.config .
RUN dotnet publish OficinaMecanica.API/OficinaMecanica.API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Executa como usuario nao-root
RUN adduser --disabled-password --gecos "" appuser
USER appuser

WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OficinaMecanica.API.dll"]
