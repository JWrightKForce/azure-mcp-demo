using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;

namespace AzureDevOpsAssistant.Security;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, RateLimitCounter> _counters;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _counters = new ConcurrentDictionary<string, RateLimitCounter>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var counter = _counters.GetOrAdd(clientId, _ => new RateLimitCounter());

        if (IsRateLimited(counter))
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        // Increment the counter
        counter.Increment();

        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        // Try to get client ID from various sources
        var clientId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(clientId))
        {
            clientId = context.Connection.RemoteIpAddress?.ToString();
        }

        if (string.IsNullOrEmpty(clientId))
        {
            clientId = "anonymous";
        }

        return clientId;
    }

    private bool IsRateLimited(RateLimitCounter counter)
    {
        var now = DateTime.UtcNow;
        
        // Clean up old entries
        counter.CleanupOldEntries(now, _options.WindowDuration);

        // Check if the limit is exceeded
        return counter.RequestCount >= _options.MaxRequests;
    }
}

public class RateLimitOptions
{
    public int MaxRequests { get; set; } = 100;
    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(1);
}

public class RateLimitCounter
{
    private readonly List<DateTime> _requests = new();
    private readonly object _lock = new();

    public int RequestCount
    {
        get
        {
            lock (_lock)
            {
                return _requests.Count;
            }
        }
    }

    public void Increment()
    {
        lock (_lock)
        {
            _requests.Add(DateTime.UtcNow);
        }
    }

    public void CleanupOldEntries(DateTime now, TimeSpan windowDuration)
    {
        lock (_lock)
        {
            var cutoff = now - windowDuration;
            _requests.RemoveAll(requestTime => requestTime < cutoff);
        }
    }
}
