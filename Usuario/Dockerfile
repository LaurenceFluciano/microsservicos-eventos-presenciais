# =====================================================
# Build
# =====================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Api/Api.csproj Api/

RUN dotnet restore Api/Api.csproj

COPY Api/ Api/

RUN dotnet publish Api/Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# =====================================================
# Runtime
# =====================================================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_CONTENTROOT=/app

EXPOSE 8080

RUN mkdir -p /data && chown app:app /app /data

COPY --from=build --chown=app:app /app/publish .

USER app

ENTRYPOINT ["dotnet", "Api.dll"]
