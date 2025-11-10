FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG GHPAT
ARG VERSION
ARG TARGETOS
ARG TARGETARCH
WORKDIR /src
COPY ["Texnokaktus.ProgOlymp.ResultService/Texnokaktus.ProgOlymp.ResultService.csproj", "Texnokaktus.ProgOlymp.ResultService/"]
COPY ["modules/Common.Contracts/Texnokaktus.ProgOlymp.Common.Contracts/Texnokaktus.ProgOlymp.Common.Contracts.csproj", "modules/Common.Contracts/Texnokaktus.ProgOlymp.Common.Contracts/"]
COPY ["Texnokaktus.ProgOlymp.Cqrs/Texnokaktus.ProgOlymp.Cqrs.csproj", "Texnokaktus.ProgOlymp.Cqrs/"]
COPY ["Texnokaktus.ProgOlymp.ResultService.DataAccess/Texnokaktus.ProgOlymp.ResultService.DataAccess.csproj", "Texnokaktus.ProgOlymp.ResultService.DataAccess/"]
COPY ["Texnokaktus.ProgOlymp.ResultService.Domain/Texnokaktus.ProgOlymp.ResultService.Domain.csproj", "Texnokaktus.ProgOlymp.ResultService.Domain/"]
COPY ["Texnokaktus.ProgOlymp.ResultService.Infrastructure/Texnokaktus.ProgOlymp.ResultService.Infrastructure.csproj", "Texnokaktus.ProgOlymp.ResultService.Infrastructure/"]
COPY ["Texnokaktus.ProgOlymp.ResultService.Logic/Texnokaktus.ProgOlymp.ResultService.Logic.csproj", "Texnokaktus.ProgOlymp.ResultService.Logic/"]
RUN dotnet nuget add source --username kav128 --password $GHPAT --store-password-in-clear-text --name "GitHub Texnokaktus-Progolymp" https://nuget.pkg.github.com/Texnokaktus-ProgOlymp/index.json
RUN dotnet restore "Texnokaktus.ProgOlymp.ResultService/Texnokaktus.ProgOlymp.ResultService.csproj" --runtime $TARGETOS-$TARGETARCH
COPY . .
WORKDIR "/src/Texnokaktus.ProgOlymp.ResultService"
RUN dotnet build "./Texnokaktus.ProgOlymp.ResultService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Texnokaktus.ProgOlymp.ResultService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:Version=$VERSION

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Texnokaktus.ProgOlymp.ResultService.dll"]
