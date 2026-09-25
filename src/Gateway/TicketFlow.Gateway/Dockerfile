# Build context: repository root (see docker-compose.yml), same as the service images.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Gateway/TicketFlow.Gateway/TicketFlow.Gateway.csproj src/Gateway/TicketFlow.Gateway/
RUN dotnet restore src/Gateway/TicketFlow.Gateway/TicketFlow.Gateway.csproj

COPY src/Gateway/ src/Gateway/
RUN dotnet publish src/Gateway/TicketFlow.Gateway/TicketFlow.Gateway.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "TicketFlow.Gateway.dll"]
