using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using test_ins.Middleware;
using test_ins.Repositories;
using test_ins.Services;
using test_ins.DTOs;
using test_ins.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryRepo>();
builder.Services.AddSingleton<IUrlService, UrlService>();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseApiKeyAuth();

app.MapPost("/users", (User user, InMemoryRepo repo) =>
{
    user.UserId = Guid.NewGuid();
    user.ApiKey = Guid.NewGuid().ToString();
    repo.AddUser(user);
    return Results.Created($"/users/{user.UserId}", user);
});

app.MapGet("/users/me", (HttpContext ctx) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();
    return Results.Ok(user);
});

app.MapPost("/urls", (ShortUrlCreate req, HttpContext ctx, IUrlService urlService) =>
{
    if (!ctx.Items.TryGetValue("User", out var u) || u is not User user)
        return Results.Unauthorized();

    try
    {
        var s = urlService.Create(user, req);
        return Results.Created($"/urls/{s.Id}", s);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = "invalid_request", detail = ex.Message });
    }
});

app.MapGet("/urls", (HttpContext ctx, InMemoryRepo repo) =>
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

app.MapGet("/r/{shortCode}", (string shortCode, IUrlService urlService) =>
{
    var s = urlService.GetByShortCode(shortCode);
    if (s == null)
        return Results.NotFound(new { error = "not_found" });

    if (s.Status != UrlStatus.Active || (s.ExpiresAt.HasValue && s.ExpiresAt.Value < DateTimeOffset.UtcNow))
    {
        return Results.StatusCode((int)HttpStatusCode.Gone);
    }

    urlService.IncrementRedirect(s);
    var response = Results.Redirect(s.Destination, true);
    return response;
});

app.Run();
