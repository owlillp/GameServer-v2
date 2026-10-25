# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG NUGET_USERNAME
WORKDIR /app

COPY ["nuget.config", "."]
COPY [".editorconfig", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Shared/", "Shared/"]
COPY ["tools/FakeGameServer/", "tools/FakeGameServer/"]

RUN --mount=type=secret,id=nuget_password \
    dotnet nuget update source github \
        --username "$NUGET_USERNAME" \
        --password "$(cat /run/secrets/nuget_password)" \
        --store-password-in-clear-text \
        --configfile /app/nuget.config \
  && dotnet restore "tools/FakeGameServer/FakeGameServer.csproj" \
  && rm -f /app/nuget.config

RUN dotnet publish "tools/FakeGameServer/FakeGameServer.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FakeGameServer.dll"]
