# PlayPredict backend — imagen productiva (P2.2).
# Multi-stage: SDK solo para compilar, runtime ASP.NET para ejecutar.
# Sin dotnet watch, sin bind mounts, sin tooling dev.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PlayPredict.Api.csproj ./
RUN dotnet restore PlayPredict.Api.csproj

COPY . .
RUN dotnet publish PlayPredict.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --retries=5 --start-period=30s \
    CMD curl -f http://localhost:8080/api/health/ready || exit 1

ENTRYPOINT ["dotnet", "PlayPredict.Api.dll"]
