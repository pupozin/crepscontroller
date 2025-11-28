# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# .dockerignore keeps Windows bin/obj artifacts (and their Windows-only project.assets.json) out of the Linux image.
COPY . .
# Restore and publish happen against the same copied sources, so there is no extra COPY between these steps that would invalidate the cache.
RUN dotnet restore
# nuget.config only references nuget.org, so restore/publish never rely on Visual Studio fallback folders that do not exist in Linux containers.
RUN dotnet publish -c Release -o /app/out

# Final
FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
# Run the published DLL directly from the output folder.
ENTRYPOINT ["dotnet", "CrepeControladorApi.dll"]
