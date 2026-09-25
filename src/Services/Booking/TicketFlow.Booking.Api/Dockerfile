# Build context: repository root (see docker-compose.yml), so shared projects under src/Shared are reachable.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: the restore layer stays cached until a .csproj changes, not on every code edit.
COPY src/Shared/TicketFlow.Contracts/TicketFlow.Contracts.csproj src/Shared/TicketFlow.Contracts/
COPY src/Services/Booking/TicketFlow.Booking.Domain/TicketFlow.Booking.Domain.csproj src/Services/Booking/TicketFlow.Booking.Domain/
COPY src/Services/Booking/TicketFlow.Booking.Application/TicketFlow.Booking.Application.csproj src/Services/Booking/TicketFlow.Booking.Application/
COPY src/Services/Booking/TicketFlow.Booking.Infrastructure/TicketFlow.Booking.Infrastructure.csproj src/Services/Booking/TicketFlow.Booking.Infrastructure/
COPY src/Services/Booking/TicketFlow.Booking.Api/TicketFlow.Booking.Api.csproj src/Services/Booking/TicketFlow.Booking.Api/
RUN dotnet restore src/Services/Booking/TicketFlow.Booking.Api/TicketFlow.Booking.Api.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Booking/ src/Services/Booking/
RUN dotnet publish src/Services/Booking/TicketFlow.Booking.Api/TicketFlow.Booking.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "TicketFlow.Booking.Api.dll"]
