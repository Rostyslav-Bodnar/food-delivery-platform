FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DF.MenuService.API/DF.MenuService.API.csproj", "DF.MenuService.API/"]
RUN dotnet restore "DF.MenuService.API/DF.MenuService.API.csproj"
COPY . .
WORKDIR "/src/DF.MenuService.API"
RUN dotnet build "./DF.MenuService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DF.MenuService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DF.MenuService.API.dll"]
