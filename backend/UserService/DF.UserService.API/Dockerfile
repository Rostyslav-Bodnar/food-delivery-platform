FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release
ARG GITHUB_TOKEN

WORKDIR /src

# copy nuget config BEFORE restore
COPY nuget.config /src/nuget.config

COPY ["DF.UserService.API/DF.UserService.API.csproj", "DF.UserService.API/"]
COPY ["DF.UserService.Application/DF.UserService.Application.csproj", "DF.UserService.Application/"]
COPY ["DF.UserService.Contracts/DF.UserService.Contracts.csproj", "DF.UserService.Contracts/"]
COPY ["DF.UserService.Domain/DF.UserService.Domain.csproj", "DF.UserService.Domain/"]
COPY ["DF.UserService.Infrastructure/DF.UserService.Infrastructure.csproj", "DF.UserService.Infrastructure/"]

COPY . .

# ONLY restore (no nuget add source!)
RUN dotnet restore "DF.UserService.API/DF.UserService.API.csproj" \
    --configfile /src/nuget.config

WORKDIR "/src/DF.UserService.API"

RUN dotnet publish "DF.UserService.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DF.UserService.API.dll"]