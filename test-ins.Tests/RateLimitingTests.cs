#if false // disabled due to test SDK compatibility with net10
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using test_ins.Middleware;
using test_ins.Models;

namespace test_ins.Tests
{
    [TestClass]
    public class RateLimitingTests
    {
        [TestMethod]
        public async Task ExceedingRateLimitReturns429()
        {
            var opts = Options.Create(new RateLimitOptions { RequestsPerMinute = 2 });
            var logger = new NullLogger<RateLimitMiddleware>();

            RequestDelegate terminal = (ctx) => { ctx.Response.StatusCode = 200; return Task.CompletedTask; };
            var middleware = new RateLimitMiddleware(terminal, opts, logger);

            var ctx = new DefaultHttpContext();
            ctx.Request.Path = "/urls";
            // attach an authenticated user in context
            ctx.Items["User"] = new User { UserId = Guid.NewGuid(), ApiKey = "k" };

            await middleware.Invoke(ctx);
            Assert.AreEqual(200, ctx.Response.StatusCode);

            // second request - still ok
            ctx = new DefaultHttpContext();
            ctx.Request.Path = "/urls";
            ctx.Items["User"] = middlewareKeyUser(ctx);
            await middleware.Invoke(ctx);
            // third request should be 429
            ctx = new DefaultHttpContext();
            ctx.Request.Path = "/urls";
            ctx.Items["User"] = middlewareKeyUser(ctx);
            await middleware.Invoke(ctx);
            Assert.AreEqual(429, ctx.Response.StatusCode);

            static User middlewareKeyUser(HttpContext _)
            {
                return new User { UserId = Guid.NewGuid(), ApiKey = "k" };
            }
        }
    }
}
#endif
