using System;
using System.Collections.Generic;
using System.Linq;
using test_ins.Models;
using test_ins.Persistence;
using Microsoft.EntityFrameworkCore;

namespace test_ins.Repositories
{
    public class PostgresRepo : IRepo
    {
        private readonly ShortenerDbContext _db;
        private readonly Microsoft.Extensions.Logging.ILogger<PostgresRepo> _logger;

        public PostgresRepo(ShortenerDbContext db, Microsoft.Extensions.Logging.ILogger<PostgresRepo> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Users
        public User? GetUserByApiKey(string apiKey)
        {
            var u = _db.Users.FirstOrDefault(u => u.ApiKey == apiKey);
            if (u == null) _logger.LogDebug("No user found for provided API key");
            return u;
        }

        public User? GetUser(Guid id) => _db.Users.Find(id);
        public IEnumerable<User> ListUsers() => _db.Users.AsNoTracking().ToList();
        public void AddUser(User user)
        {
            _db.Users.Add(user);
            _db.SaveChanges();
            _logger.LogInformation("Added user {UserId}", user.UserId);
        }

        public void UpdateUser(User user)
        {
            _db.Users.Update(user);
            _db.SaveChanges();
            _logger.LogInformation("Updated user {UserId}", user.UserId);
        }

        // ShortUrls
        public ShortUrl CreateShortUrl(ShortUrl s)
        {
            _db.ShortUrls.Add(s);
            _db.SaveChanges();
            return s;
        }

        public ShortUrl? GetShortUrl(Guid id) => _db.ShortUrls.Find(id);

        public ShortUrl? GetByShortCode(string shortCode) => _db.ShortUrls.FirstOrDefault(s => s.ShortCode == shortCode);

        public IEnumerable<ShortUrl> ListByOwner(Guid ownerId) => _db.ShortUrls.Where(u => u.OwnerUserId == ownerId).AsNoTracking().ToList();

        public void UpdateShortUrl(ShortUrl s)
        {
            _db.ShortUrls.Update(s);
            _db.SaveChanges();
        }

        public void DeleteShortUrl(Guid id)
        {
            var s = _db.ShortUrls.Find(id);
            if (s != null)
            {
                // also remove related redirect events
                var events = _db.RedirectEvents.Where(e => e.ShortUrlId == id).ToList();
                if (events.Any())
                {
                    _db.RedirectEvents.RemoveRange(events);
                }

                // also remove related audit events
                var audits = _db.AuditEvents.Where(a => a.TargetEntityId == id && a.TargetEntityType == "ShortUrl").ToList();
                if (audits.Any())
                {
                    _db.AuditEvents.RemoveRange(audits);
                }

                _db.ShortUrls.Remove(s);
                _db.SaveChanges();
            }
        }

        public void AddRedirectEvent(Models.RedirectEvent e)
        {
            _db.RedirectEvents.Add(e);
            _db.SaveChanges();
        }

        public System.Collections.Generic.IEnumerable<Models.RedirectEvent> ListRedirectEvents(Guid shortUrlId, DateTimeOffset since)
        {
            return _db.RedirectEvents.Where(e => e.ShortUrlId == shortUrlId && e.Timestamp >= since).OrderBy(e => e.Timestamp).AsNoTracking().ToList();
        }

        public void AddAuditEvent(Models.AuditEvent e)
        {
            _db.AuditEvents.Add(e);
            _db.SaveChanges();
        }

        public System.Collections.Generic.IEnumerable<Models.AuditEvent> ListAuditEvents(string targetEntityType, Guid targetEntityId, DateTimeOffset since)
        {
            return _db.AuditEvents.Where(a => a.TargetEntityType == targetEntityType && a.TargetEntityId == targetEntityId && a.Timestamp >= since).OrderBy(a => a.Timestamp).AsNoTracking().ToList();
        }

        // Rate tier operations
        public Models.RateTierEntity CreateRateTier(Models.RateTierEntity r)
        {
            _db.Add(r);
            _db.SaveChanges();
            return r;
        }

        public Models.RateTierEntity? GetRateTier(Guid id) => _db.Set<Models.RateTierEntity>().Find(id);
        public Models.RateTierEntity? GetRateTierByName(string name) => _db.Set<Models.RateTierEntity>().FirstOrDefault(t => t.Name == name);
        public System.Collections.Generic.IEnumerable<Models.RateTierEntity> ListRateTiers() => _db.Set<Models.RateTierEntity>().AsNoTracking().ToList();
        public void UpdateRateTier(Models.RateTierEntity r)
        {
            _db.Set<Models.RateTierEntity>().Update(r);
            _db.SaveChanges();
        }

        public void DeleteRateTier(Guid id)
        {
            var r = _db.Set<Models.RateTierEntity>().Find(id);
            if (r != null)
            {
                _db.Set<Models.RateTierEntity>().Remove(r);
                _db.SaveChanges();
            }
        }

        public void IncrementRedirect(ShortUrl s)
        {
            s.RedirectCount++;
            s.UpdatedAt = DateTimeOffset.UtcNow;
            _db.ShortUrls.Update(s);
            _db.SaveChanges();

            // add redirect event row for analytics
            var ev = new Models.RedirectEvent { ShortUrlId = s.Id, Timestamp = DateTimeOffset.UtcNow };
            _db.RedirectEvents.Add(ev);
            _db.SaveChanges();
        }
    }
}
