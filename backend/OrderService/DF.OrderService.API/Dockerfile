FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG GITHUB_TOKEN
WORKDIR /src

# Authenticate to the private GitHub NuGet feed (DF.Contracts).
RUN dotnet nuget remove source github || true && \
    dotnet nuget add source \
    --username "Rostyslav-Bodnar" \
    --password $GITHUB_TOKEN \
    --store-password-in-clear-text \
    --name github \
    "https://nuget.pkg.github.com/Rostyslav-Bodnar/index.json"

COPY ["DF.OrderService.API/DF.OrderService.API.csproj", "DF.OrderService.API/"]
COPY . .
WORKDIR "/src/DF.OrderService.API"
RUN dotnet build "./DF.OrderService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DF.OrderService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DF.OrderService.API.dll"]
