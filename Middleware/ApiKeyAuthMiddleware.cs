using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using test_ins.Repositories;

namespace test_ins.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly InMemoryRepo _repo;

        public ApiKeyAuthMiddleware(RequestDelegate next, InMemoryRepo repo)
        {
            _next = next;
            _repo = repo;
        }

        public async Task Invoke(HttpContext context)
        {
            // allow anonymous for redirect path
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/r/", System.StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "missing_api_key" });
                return;
            }

            var user = _repo.GetUserByApiKey(key.ToString());
            if (user == null || user.Status != test_ins.Models.UserStatus.Active)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "invalid_api_key_or_suspended_user" });
                return;
            }

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
