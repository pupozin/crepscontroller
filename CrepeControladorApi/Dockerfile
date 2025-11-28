# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["CrepeControladorApi.csproj", "./"]
RUN dotnet restore "CrepeControladorApi.csproj"

COPY . .
RUN rm -rf bin obj
RUN dotnet restore "CrepeControladorApi.csproj"
RUN dotnet publish "CrepeControladorApi.csproj" -c Release -o /app/publish

# Final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CrepeControladorApi.dll"]
