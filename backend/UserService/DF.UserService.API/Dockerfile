FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY . .

RUN dotnet restore "DF.UserService.API/DF.UserService.API.csproj"

WORKDIR "/src/DF.UserService.API"

RUN dotnet publish "./DF.UserService.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DF.UserService.API.dll"]