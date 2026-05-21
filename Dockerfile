FROM curlimages/curl:latest AS dd-tracer
USER root
ARG DD_TRACER_VERSION=2.55.0
RUN curl -Lo /tmp/dd-tracer.tar.gz \
    "https://github.com/DataDog/dd-trace-dotnet/releases/download/v${DD_TRACER_VERSION}/datadog-dotnet-apm-${DD_TRACER_VERSION}-musl.tar.gz" \
 && mkdir -p /opt/datadog \
 && tar -C /opt/datadog -xzf /tmp/dd-tracer.tar.gz

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY ["PaymentsAPI.sln", "."]
COPY ["src/Payments.Domain/Payments.Domain.csproj", "src/Payments.Domain/"]
COPY ["src/Payments.Application/Payments.Application.csproj", "src/Payments.Application/"]
COPY ["src/Payments.Infrastructure/Payments.Infrastructure.csproj", "src/Payments.Infrastructure/"]
COPY ["src/Payments.API/Payments.API.csproj", "src/Payments.API/"]
RUN dotnet restore "src/Payments.API/Payments.API.csproj"

COPY . .
RUN dotnet publish "src/Payments.API/Payments.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apk upgrade --no-cache
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=dd-tracer /opt/datadog /opt/datadog
USER app
ENTRYPOINT ["dotnet", "Payments.API.dll"]
