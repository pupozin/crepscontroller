# =========================
# Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

# a porta interna do container
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# =========================
# Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# copia só o csproj primeiro (cache de restore)
COPY CrepeControladorApi/CrepeControladorApi.csproj CrepeControladorApi/
RUN dotnet restore CrepeControladorApi/CrepeControladorApi.csproj

# agora copia o resto
COPY . .
RUN dotnet publish CrepeControladorApi -c Release -o /app/out

# =========================
# Final
# =========================
FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "CrepeControladorApi.dll"]
