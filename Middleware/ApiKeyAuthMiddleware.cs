using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using test_ins.Repositories;

namespace test_ins.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Repositories.IRepo _repo;
        private readonly Microsoft.Extensions.Logging.ILogger<ApiKeyAuthMiddleware> _logger;

        public ApiKeyAuthMiddleware(RequestDelegate next, Repositories.IRepo repo, Microsoft.Extensions.Logging.ILogger<ApiKeyAuthMiddleware> logger)
        {
            _next = next;
            _repo = repo;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // allow anonymous for redirect path and health checks
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/r/", System.StringComparison.OrdinalIgnoreCase) || path.Equals("/health", System.StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key))
            {
                _logger.LogWarning("Missing API key for request to {Path}", path);
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "missing_api_key" });
                return;
            }

            var user = _repo.GetUserByApiKey(key.ToString());
            if (user == null || user.Status != test_ins.Models.UserStatus.Active)
            {
                _logger.LogWarning("Invalid or suspended API key attempt to {Path}", path);
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "invalid_api_key_or_suspended_user" });
                return;
            }

            _logger.LogDebug("Authenticated user {UserId} for request to {Path}", user.UserId, path);

            // attach user id to context for handlers
            context.Items["User"] = user;

            await _next(context);
        }
    }

    public static class ApiKeyAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiKeyAuthMiddleware>();
        }
    }
}
