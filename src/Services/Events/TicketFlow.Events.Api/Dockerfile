# Build context: repository root (see docker-compose.yml), so shared projects under src/Shared are reachable.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: the restore layer stays cached until a .csproj changes, not on every code edit.
COPY src/Shared/TicketFlow.Contracts/TicketFlow.Contracts.csproj src/Shared/TicketFlow.Contracts/
COPY src/Services/Events/TicketFlow.Events.Domain/TicketFlow.Events.Domain.csproj src/Services/Events/TicketFlow.Events.Domain/
COPY src/Services/Events/TicketFlow.Events.Application/TicketFlow.Events.Application.csproj src/Services/Events/TicketFlow.Events.Application/
COPY src/Services/Events/TicketFlow.Events.Infrastructure/TicketFlow.Events.Infrastructure.csproj src/Services/Events/TicketFlow.Events.Infrastructure/
COPY src/Services/Events/TicketFlow.Events.Api/TicketFlow.Events.Api.csproj src/Services/Events/TicketFlow.Events.Api/
RUN dotnet restore src/Services/Events/TicketFlow.Events.Api/TicketFlow.Events.Api.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Events/ src/Services/Events/
RUN dotnet publish src/Services/Events/TicketFlow.Events.Api/TicketFlow.Events.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# The aspnet image listens on 8080 and ships a non-root "app" user.
USER $APP_UID
ENTRYPOINT ["dotnet", "TicketFlow.Events.Api.dll"]
