using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Application.DTOs;

namespace TicketFlow.Events.Infrastructure.Caching;

public class RedisPublishedEventsCache(
    IConnectionMultiplexer redis,
    ILogger<RedisPublishedEventsCache> logger) : IPublishedEventsCache
{
    private const string Key = "events:published";

    public async Task<List<EventSummaryDto>?> GetAsync(CancellationToken ct = default)
    {
        try
        {
            var cached = await redis.GetDatabase().StringGetAsync(Key);
            if (cached.IsNullOrEmpty)
            {
                logger.LogDebug("Cache miss for {CacheKey}", Key);
                return null;
            }

            logger.LogDebug("Cache hit for {CacheKey}", Key);
            return JsonSerializer.Deserialize<List<EventSummaryDto>>(cached.ToString());
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            logger.LogWarning(ex, "Redis read of {CacheKey} failed, falling back to the database", Key);
            return null;
        }
    }

    // No TTL: the key is invalidated explicitly whenever the published list changes.
    public async Task SetAsync(List<EventSummaryDto> events, CancellationToken ct = default)
    {
        try
        {
            await redis.GetDatabase().StringSetAsync(Key, JsonSerializer.Serialize(events));
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            logger.LogWarning(ex, "Redis write of {CacheKey} failed, response served uncached", Key);
        }
    }

    public async Task InvalidateAsync(CancellationToken ct = default)
    {
        try
        {
            await redis.GetDatabase().KeyDeleteAsync(Key);
            logger.LogDebug("Invalidated {CacheKey}", Key);
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            logger.LogWarning(ex, "Redis invalidation of {CacheKey} failed - cached list may be stale until the next invalidation", Key);
        }
    }

    // RedisTimeoutException derives from TimeoutException, not RedisException, so both are needed.
    private static bool IsCacheFailure(Exception ex) => ex is RedisException or TimeoutException or JsonException;
}
