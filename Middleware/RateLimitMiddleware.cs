using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace test_ins.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitOptions _opts;
        private readonly Microsoft.Extensions.Logging.ILogger<RateLimitMiddleware> _logger;

        // simple fixed-window counter per key
        private readonly ConcurrentDictionary<string, (DateTimeOffset windowStart, int count)> _counters = new();

        public RateLimitMiddleware(RequestDelegate next, IOptions<RateLimitOptions> opts, Microsoft.Extensions.Logging.ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            // skip excluded paths
            foreach (var p in _opts.ExcludedPathPrefixes)
            {
                if (path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }
            }

            // require authenticated user (middleware runs after ApiKeyAuth)
            if (!context.Items.TryGetValue("User", out var u) || u is not test_ins.Models.User user)
            {
                // for unauthenticated management endpoints, block
                _logger.LogWarning("Rate limit: unauthenticated request to {Path}", path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "missing_api_key" });
                return;
            }

            var key = user.UserId.ToString();
            var now = DateTimeOffset.UtcNow;
            var windowKey = key;

            _counters.AddOrUpdate(windowKey,
                k => (now, 1),
                (k, old) =>
                {
                    // if still in same minute window
                    if ((now - old.windowStart).TotalSeconds < 60)
                    {
                        return (old.windowStart, old.count + 1);
                    }
                    // reset window
                    return (now, 1);
                });

            var cur = _counters[windowKey];

            // honor per-user rate limit if set, otherwise use global
            var limit = user.RateLimitRpm ?? _opts.RequestsPerMinute;
            if (cur.count > limit)
            {
                var retryAfter = 60 - (int)(now - cur.windowStart).TotalSeconds;
                _logger.LogWarning("Rate limit exceeded for user {UserId} on {Path}", user.UserId, path);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = retryAfter.ToString();
                await context.Response.WriteAsJsonAsync(new { error = "rate_limited", retry_after = retryAfter });
                return;
            }

            await _next(context);
        }
    }

    public static class RateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimitMiddleware>();
        }
    }
}
