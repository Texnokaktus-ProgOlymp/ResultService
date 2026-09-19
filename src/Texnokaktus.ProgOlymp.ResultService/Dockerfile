FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
ENV SERVICE_NAME=result-service
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION
ARG TARGETOS
ARG TARGETARCH
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY ["modules/Common.Contracts/Directory.Packages.props", "modules/Common.Contracts/"]
COPY ["Directory.Build.props", "./"]
COPY ["src/Directory.Build.props", "src/"]
COPY ["src/Texnokaktus.ProgOlymp.ResultService/Texnokaktus.ProgOlymp.ResultService.csproj", "src/Texnokaktus.ProgOlymp.ResultService/"]
COPY ["modules/Common.Contracts/Texnokaktus.ProgOlymp.Common.Contracts/Texnokaktus.ProgOlymp.Common.Contracts.csproj", "modules/Common.Contracts/Texnokaktus.ProgOlymp.Common.Contracts/"]
COPY ["src/Texnokaktus.ProgOlymp.ResultService.DataAccess/Texnokaktus.ProgOlymp.ResultService.DataAccess.csproj", "src/Texnokaktus.ProgOlymp.ResultService.DataAccess/"]
COPY ["src/Texnokaktus.ProgOlymp.ResultService.Domain/Texnokaktus.ProgOlymp.ResultService.Domain.csproj", "src/Texnokaktus.ProgOlymp.ResultService.Domain/"]
COPY ["src/Texnokaktus.ProgOlymp.ResultService.Infrastructure/Texnokaktus.ProgOlymp.ResultService.Infrastructure.csproj", "src/Texnokaktus.ProgOlymp.ResultService.Infrastructure/"]
RUN --mount=type=secret,id=GHPAT dotnet nuget add source --username kav128 --password "$(cat /run/secrets/GHPAT)" --store-password-in-clear-text --name "GitHub Texnokaktus-Progolymp" https://nuget.pkg.github.com/Texnokaktus-ProgOlymp/index.json
WORKDIR "/src/src"
RUN dotnet restore "./Texnokaktus.ProgOlymp.ResultService/Texnokaktus.ProgOlymp.ResultService.csproj" --runtime $TARGETOS-$TARGETARCH --verbosity diagnostic
COPY . .
WORKDIR "/src/src/Texnokaktus.ProgOlymp.ResultService"
RUN dotnet build "./Texnokaktus.ProgOlymp.ResultService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Texnokaktus.ProgOlymp.ResultService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:Version=$VERSION

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Texnokaktus.ProgOlymp.ResultService.dll"]
