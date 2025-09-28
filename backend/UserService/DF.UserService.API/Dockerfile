FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DF.UserService.API/DF.UserService.API.csproj", "DF.UserService.API/"]
COPY ["DF.UserService.Application/DF.UserService.Application.csproj", "DF.UserService.Application/"]
COPY ["DF.UserService.Contracts/DF.UserService.Contracts.csproj", "DF.UserService.Contracts/"]
COPY ["DF.UserService.Domain/DF.UserService.Domain.csproj", "DF.UserService.Domain/"]
COPY ["DF.UserService.Infrastructure/DF.UserService.Infrastructure.csproj", "DF.UserService.Infrastructure/"]
RUN dotnet restore "DF.UserService.API/DF.UserService.API.csproj"
COPY . .
WORKDIR "/src/DF.UserService.API"
RUN dotnet build "./DF.UserService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DF.UserService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DF.UserService.API.dll"]
