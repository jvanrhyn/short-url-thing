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
    return Results.Created($"/users/{user.UserId}", user);
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

    s.Status = UrlStatus.Deleted;
    urlService.Update(s, new ShortUrlUpdate { });
    return Results.NoContent();
});

app.MapGet("/urls/{id}/stats", (Guid id, HttpContext ctx, IUrlService urlService) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    var s = urlService.GetById(id);
    if (s == null || s.OwnerUserId != user.UserId)
        return Results.NotFound(new { error = "not_found" });

    return Results.Ok(new { redirects = s.RedirectCount, createdAt = s.CreatedAt, updatedAt = s.UpdatedAt });
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

app.Run();
