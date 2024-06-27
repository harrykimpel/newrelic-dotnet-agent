ARG DOTNET_VERSION
ARG DISTRO_TAG
ARG TARGET_ARCH
FROM --platform=${TARGET_ARCH} mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-${DISTRO_TAG} AS base
WORKDIR /app
EXPOSE 80

# build image is always amd64 (to match the runner architecture), even though the target architecture may be arm64
ARG DOTNET_VERSION
FROM --platform=amd64 mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-${DISTRO_TAG} AS build 
ARG TARGET_ARCH
WORKDIR /src
COPY ["SocketMultiFunctionApplicationCore/SocketMultiFunctionApplicationCore.csproj", "ContainerApplications/SocketMultiFunctionApplicationCore/"]
COPY --from=tests ./Agent/IntegrationTests/SharedApplications/Common SharedApplications/Common
COPY --from=src ./Agent/NewRelic.Api.Agent ./Agent/NewRelic.Api.Agent
COPY --from=tests ./Agent/IntegrationTests/Shared ./Shared
COPY --from=root ./nuget.config ./nuget.config
copy --from=tests ./Agent/IntegrationTests/nuget.config ./SharedApplications/nuget.config

RUN ls -l
RUN dotnet restore "ContainerApplications/SocketMultiFunctionApplicationCore/SocketMultiFunctionApplicationCore.csproj" -a ${TARGET_ARCH}

COPY . .
WORKDIR "/src/ContainerApplications/SocketMultiFunctionApplicationCore"
RUN dotnet build "./SocketMultiFunctionApplicationCore.csproj"  -c Release -o /app/build --os linux -a ${TARGET_ARCH}

FROM build AS publish
RUN dotnet publish "./SocketMultiFunctionApplicationCore.csproj" -c Release -o /app/publish /p:UseAppHost=false --os linux -a ${TARGET_ARCH}

FROM base AS final

# Enable the agent
ARG NEW_RELIC_HOST
ARG NEW_RELIC_LICENSE_KEY
ARG NEW_RELIC_APP_NAME

ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
NEW_RELIC_HOST=${NEW_RELIC_HOST} \
NEW_RELIC_LICENSE_KEY=${NEW_RELIC_LICENSE_KEY} \
NEW_RELIC_APP_NAME=${NEW_RELIC_APP_NAME} \
NEWRELIC_LOG_DIRECTORY=/app/logs

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SocketMultiFunctionApplicationCore.dll localhost 80"]
