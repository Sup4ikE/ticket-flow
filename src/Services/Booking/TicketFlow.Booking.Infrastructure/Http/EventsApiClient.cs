using System.Net;
using System.Net.Http.Json;
using TicketFlow.Booking.Application.Abstractions;
using TicketFlow.Booking.Application.Exceptions;

namespace TicketFlow.Booking.Infrastructure.Http;

public class EventsApiClient(HttpClient httpClient) : IEventCatalog
{
    public async Task<EventInfo?> GetEventAsync(Guid eventId, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.GetAsync($"api/events/{eventId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
                throw new EventsServiceUnavailableException(
                    $"Events service returned {(int)response.StatusCode} for event {eventId}.");

            return await response.Content.ReadFromJsonAsync<EventInfo>(ct)
                ?? throw new EventsServiceUnavailableException($"Events service returned an empty body for event {eventId}.");
        }
        catch (HttpRequestException ex)
        {
            throw new EventsServiceUnavailableException("Events service is unreachable.", ex);
        }
        // HttpClient.Timeout surfaces as TaskCanceledException; a genuine caller cancellation is left to propagate.
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new EventsServiceUnavailableException("Events service did not respond in time.", ex);
        }
    }
}
