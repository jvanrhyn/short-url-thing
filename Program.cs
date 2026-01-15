using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using test_ins.Middleware;
using test_ins.Repositories;
using test_ins.Services;
using test_ins.DTOs;
using test_ins.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration - read from appsettings and wire into Generic Host
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Register repository: prefer Postgres when a connection string is configured, otherwise fall back to in-memory for local dev.
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(conn))
{
    builder.Services.AddDbContext<test_ins.Persistence.ShortenerDbContext>(options => options.UseNpgsql(conn));
    builder.Services.AddScoped<test_ins.Repositories.IRepo, test_ins.Repositories.PostgresRepo>();
    builder.Services.AddScoped<IUrlService, UrlService>();
}
else
{
    builder.Services.AddSingleton<test_ins.Repositories.IRepo, test_ins.Repositories.InMemoryRepo>();
    builder.Services.AddScoped<IUrlService, UrlService>();
}


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key needed to access endpoints. Use X-Api-Key header.",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
});

// configure rate limiting options from configuration
builder.Services.Configure<test_ins.Middleware.RateLimitOptions>(builder.Configuration.GetSection("RateLimiting"));

// register available rate tiers (simple in-memory list for now)
var rateTiers = new List<test_ins.Models.RateTier>
{
    new test_ins.Models.RateTier { Name = "free", RequestsPerMinute = 60, Description = "Free tier - 60 rpm" },
    new test_ins.Models.RateTier { Name = "team", RequestsPerMinute = 300, Description = "Team tier - 300 rpm" },
    new test_ins.Models.RateTier { Name = "enterprise", RequestsPerMinute = 2000, Description = "Enterprise tier - 2000 rpm" }
};
builder.Services.AddSingleton(rateTiers);


var app = builder.Build();

try
{
    Log.Information("Starting application");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseApiKeyAuth();

    // Rate limiting middleware (should run after API key auth so we can enforce per-user limits)
    app.UseRateLimiting();

    // Apply pending EF Core migrations at startup when Postgres is configured (safe for dev/demo use). In production consider an explicit migration pipeline.
    using (var scope = app.Services.CreateScope())
    {
        var cfg = scope.ServiceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
        var cs = cfg?.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(cs))
        {
            var db = scope.ServiceProvider.GetRequiredService<test_ins.Persistence.ShortenerDbContext>();
            db.Database.Migrate();

            // Seed a development user if none exist to simplify local testing under Postgres.
            if (!db.Users.Any())
            {
                db.Users.Add(new test_ins.Models.User { Email = "dev@example.local", ApiKey = "dev-api-key-123" });
                db.SaveChanges();
            }
        }
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception during startup");
    throw;
}
finally
{
    // Ensure logs are flushed when application stops
    AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();
}

app.Lifetime.ApplicationStopped.Register(() => Log.CloseAndFlush());

// Apply pending EF Core migrations at startup when Postgres is configured (safe for dev/demo use). In production consider an explicit migration pipeline.
using (var scope = app.Services.CreateScope())
{
    var cfg = scope.ServiceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
    var cs = cfg?.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(cs))
    {
        var db = scope.ServiceProvider.GetRequiredService<test_ins.Persistence.ShortenerDbContext>();
        db.Database.Migrate();

        // Seed a development user if none exist to simplify local testing under Postgres.
        if (!db.Users.Any())
        {
            db.Users.Add(new test_ins.Models.User { Email = "dev@example.local", ApiKey = "dev-api-key-123" });
            db.SaveChanges();
        }
    }
}

// Note: for Postgres users, set the connection string under "ConnectionStrings:DefaultConnection" in appsettings or environment variables.


app.MapPost("/users", (User user, IRepo repo, ILogger<Program> logger) =>
{
    user.UserId = Guid.NewGuid();
    user.ApiKey = Guid.NewGuid().ToString();
    repo.AddUser(user);
    logger.LogInformation("Created new user {UserId}", user.UserId);

    // audit (system actor)
    repo.AddAuditEvent(new test_ins.Models.AuditEvent
    {
        ActorUserId = null,
        Action = "user:create",
        TargetEntityType = "User",
        TargetEntityId = user.UserId,
        Details = $"{{\"email\":\"{user.Email}\"}}"
    });

    return Results.Created($"/users/{user.UserId}", user);
});

// Rate tier endpoints
app.MapGet("/rate-tiers", (IEnumerable<test_ins.Models.RateTier> tiers) => Results.Ok(tiers));

app.MapPatch("/users/me/rate-limit", (RateLimitRequest body, HttpContext ctx, IRepo repo, IEnumerable<test_ins.Models.RateTier> tiers) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    var tier = tiers.FirstOrDefault(t => string.Equals(t.Name, body.TierName, StringComparison.OrdinalIgnoreCase));
    if (tier == null)
        return Results.BadRequest(new { error = "invalid_tier" });

    user.RateLimitRpm = tier.RequestsPerMinute;
    repo.UpdateUser(user);
    return Results.Ok(new { rate_limit_rpm = user.RateLimitRpm, tier = tier.Name });
});

app.MapGet("/users/me", (HttpContext ctx) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();
    return Results.Ok(user);
});

app.MapPost("/urls", (ShortUrlCreate req, HttpContext ctx, IUrlService urlService, ILogger<Program> logger) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    try
    {
        var s = urlService.Create(user, req);
        logger.LogInformation("User {UserId} created short url {ShortCode} (id={Id})", user.UserId, s.ShortCode, s.Id);
        return Results.Created($"/urls/{s.Id}", s);
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning("User {UserId} provided invalid url create request: {Detail}", user.UserId, ex.Message);
        return Results.BadRequest(new { error = "invalid_request", detail = ex.Message });
    }
});

app.MapGet("/urls", (HttpContext ctx, IRepo repo) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    var list = repo.ListByOwner(user.UserId);
    return Results.Ok(list);
});

app.MapGet("/urls/{id}", (Guid id, HttpContext ctx, IUrlService urlService) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    var s = urlService.GetById(id);
    if (s == null || s.OwnerUserId != user.UserId)
        return Results.NotFound(new { error = "not_found" });
    return Results.Ok(s);
});

app.MapPatch("/urls/{id}", (Guid id, ShortUrlUpdate update, HttpContext ctx, IUrlService urlService) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    var s = urlService.GetById(id);
    if (s == null || s.OwnerUserId != user.UserId)
        return Results.NotFound(new { error = "not_found" });

    urlService.Update(s, update);
    return Results.Ok(s);
});

app.MapDelete("/urls/{id}", (Guid id, HttpContext ctx, IUrlService urlService) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    var s = urlService.GetById(id);
    if (s == null || s.OwnerUserId != user.UserId)
        return Results.NotFound(new { error = "not_found" });

    urlService.Delete(id);
    return Results.NoContent();
});

app.MapDelete("/urls/{id}", (Guid id, HttpContext ctx, IUrlService urlService) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    var s = urlService.GetById(id);
    if (s == null || s.OwnerUserId != user.UserId)
        return Results.NotFound(new { error = "not_found" });

    s.Status = UrlStatus.Deleted;
    urlService.Update(s, new ShortUrlUpdate { });
    return Results.NoContent();
});

app.MapGet("/urls/{id}/stats", (Guid id, HttpContext ctx, IUrlService urlService, IRepo repo) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    var s = urlService.GetById(id);
    if (s == null || s.OwnerUserId != user.UserId)
        return Results.NotFound(new { error = "not_found" });

    // provide last-7-days daily aggregation
    var since = DateTimeOffset.UtcNow.AddDays(-7);
    var events = repo.ListRedirectEvents(id, since);
    var daily = events.GroupBy(e => e.Timestamp.UtcDateTime.Date)
                      .Select(g => new { date = g.Key, count = g.Count() })
                      .OrderBy(d => d.date)
                      .ToList();

    // also expose recent audit events for the short url (last 7 days)
    var audits = repo.ListAuditEvents("ShortUrl", id, since)
                    .Select(a => new { a.Timestamp, a.Action, a.ActorUserId, a.Details });

    return Results.Ok(new { redirects = s.RedirectCount, createdAt = s.CreatedAt, updatedAt = s.UpdatedAt, dailyCounts = daily, audits });
});

app.MapGet("/r/{shortCode}", (string shortCode, IUrlService urlService, ILogger<Program> logger) =>
{
    var s = urlService.GetByShortCode(shortCode);
    if (s == null)
    {
        logger.LogInformation("Redirect requested for unknown shortCode {ShortCode}", shortCode);
        return Results.NotFound(new { error = "not_found" });
    }

    if (s.Status != UrlStatus.Active || (s.ExpiresAt.HasValue && s.ExpiresAt.Value < DateTimeOffset.UtcNow))
    {
        logger.LogInformation("Blocked redirect for shortCode {ShortCode} due to status {Status}", shortCode, s.Status);
        return Results.StatusCode((int)HttpStatusCode.Gone);
    }

    urlService.IncrementRedirect(s);
    logger.LogInformation("Redirecting shortCode {ShortCode} to {Destination} (owner={Owner})", s.ShortCode, s.Destination, s.OwnerUserId);
    var response = Results.Redirect(s.Destination, true);
    return response;
});

// Health endpoint used by docker-compose healthcheck (no auth required)
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Audit endpoints (authenticated)
app.MapGet("/audits/shorturl/{id}", (Guid id, HttpContext ctx, IRepo repo) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    var s = repo.GetShortUrl(id);
    if (s == null || s.OwnerUserId != user.UserId)
        return Results.NotFound(new { error = "not_found" });

    var since = DateTimeOffset.UtcNow.AddDays(-7);
    var audits = repo.ListAuditEvents("ShortUrl", id, since)
                    .Select(a => new { a.Timestamp, a.Action, a.ActorUserId, a.Details });
    return Results.Ok(audits);
});

app.MapGet("/audits/user/{id}", (Guid id, HttpContext ctx, IRepo repo) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    if (user.UserId != id)
        return Results.Forbid();

    var since = DateTimeOffset.UtcNow.AddDays(-7);
    var audits = repo.ListAuditEvents("User", id, since)
                    .Select(a => new { a.Timestamp, a.Action, a.ActorUserId, a.Details });
    return Results.Ok(audits);
});

app.Run();
