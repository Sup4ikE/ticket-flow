# Build context: repository root (see docker-compose.yml), so shared projects under src/Shared are reachable.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Shared/TicketFlow.Contracts/TicketFlow.Contracts.csproj src/Shared/TicketFlow.Contracts/
COPY src/Services/Notification/TicketFlow.Notification.Api/TicketFlow.Notification.Api.csproj src/Services/Notification/TicketFlow.Notification.Api/
RUN dotnet restore src/Services/Notification/TicketFlow.Notification.Api/TicketFlow.Notification.Api.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Notification/ src/Services/Notification/
RUN dotnet publish src/Services/Notification/TicketFlow.Notification.Api/TicketFlow.Notification.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "TicketFlow.Notification.Api.dll"]
