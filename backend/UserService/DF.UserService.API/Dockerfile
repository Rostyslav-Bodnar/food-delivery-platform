# =========================
# BASE RUNTIME
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release
ARG GITHUB_USERNAME
ARG GITHUB_TOKEN

WORKDIR /src

# kerberos dependency for github packages auth
RUN apt-get update && \
    apt-get install -y libgssapi-krb5-2 && \
    rm -rf /var/lib/apt/lists/*

# =========================
# COPY NUGET CONFIG
# =========================
COPY nuget.config ./nuget.config

# =========================
# COPY CSPROJ FILES
# =========================
COPY DF.UserService.API/DF.UserService.API.csproj DF.UserService.API/
COPY DF.UserService.Application/DF.UserService.Application.csproj DF.UserService.Application/
COPY DF.UserService.Contracts/DF.UserService.Contracts.csproj DF.UserService.Contracts/
COPY DF.UserService.Domain/DF.UserService.Domain.csproj DF.UserService.Domain/
COPY DF.UserService.Infrastructure/DF.UserService.Infrastructure.csproj DF.UserService.Infrastructure/

# =========================
# AUTH FOR GITHUB PACKAGES
# =========================
RUN sed -i "s|%GITHUB_USERNAME%|${GITHUB_USERNAME}|g" /src/nuget.config && \
    sed -i "s|%GITHUB_TOKEN%|${GITHUB_TOKEN}|g" /src/nuget.config

# =========================
# RESTORE
# =========================
RUN dotnet restore "DF.UserService.API/DF.UserService.API.csproj" \
    --configfile /src/nuget.config

# =========================
# COPY SOURCE
# =========================
COPY . .

WORKDIR /src/DF.UserService.API

# =========================
# PUBLISH
# =========================
RUN dotnet publish "DF.UserService.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# FINAL IMAGE
# =========================
FROM base AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DF.UserService.API.dll"]