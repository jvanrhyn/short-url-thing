using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using test_ins.Models;
using System.Collections;

namespace test_ins.Repositories
{
    public class InMemoryRepo : IRepo
    {
        private readonly ConcurrentDictionary<Guid, User> _users = new();
        private readonly ConcurrentDictionary<Guid, ShortUrl> _urls = new();
        private readonly ConcurrentDictionary<string, Guid> _shortCodeIndex = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<Guid, System.Collections.Concurrent.ConcurrentBag<Models.RedirectEvent>> _redirectEvents = new();
        private readonly ConcurrentDictionary<Guid, System.Collections.Concurrent.ConcurrentBag<Models.AuditEvent>> _auditEvents = new();
        private readonly ConcurrentDictionary<Guid, Models.RateTierEntity> _rateTiers = new();

        // seed some default tiers
        private void SeedDefaultTiers()
        {
            var free = new Models.RateTierEntity { Name = "free", RequestsPerMinute = 60, Description = "Free tier" };
            var team = new Models.RateTierEntity { Name = "team", RequestsPerMinute = 300, Description = "Team tier" };
            var enterprise = new Models.RateTierEntity { Name = "enterprise", RequestsPerMinute = 2000, Description = "Enterprise tier" };
            _rateTiers[free.Id] = free;
            _rateTiers[team.Id] = team;
            _rateTiers[enterprise.Id] = enterprise;
        }
        public InMemoryRepo()
        {
            // seed a default user for local dev
            var user = new User
            {
                Email = "dev@example.local",
                ApiKey = "dev-api-key-123",
                IsAdmin = true
            };
            _users[user.UserId] = user;

            SeedDefaultTiers();
        }

        // Users
        public User? GetUserByApiKey(string apiKey) => _users.Values.FirstOrDefault(u => u.ApiKey == apiKey);
        public User? GetUser(Guid id) => _users.TryGetValue(id, out var u) ? u : null;
        public IEnumerable<User> ListUsers() => _users.Values;
        public void AddUser(User user) => _users[user.UserId] = user;
        public void UpdateUser(User user) => _users[user.UserId] = user;

        // ShortUrls
        public ShortUrl CreateShortUrl(ShortUrl s)
        {
            _urls[s.Id] = s;
            _shortCodeIndex[s.ShortCode] = s.Id;
            return s;
        }

        public ShortUrl? GetShortUrl(Guid id) => _urls.TryGetValue(id, out var s) ? s : null;

        public ShortUrl? GetByShortCode(string shortCode)
        {
            if (_shortCodeIndex.TryGetValue(shortCode, out var id) && _urls.TryGetValue(id, out var s))
                return s;
            return null;
        }

        public IEnumerable<ShortUrl> ListByOwner(Guid ownerId) => _urls.Values.Where(u => u.OwnerUserId == ownerId);

        public void UpdateShortUrl(ShortUrl s)
        {
            s.UpdatedAt = System.DateTimeOffset.UtcNow;
            _urls[s.Id] = s;
        }

        public void DeleteShortUrl(Guid id)
        {
            if (_urls.TryRemove(id, out var s))
            {
                _shortCodeIndex.TryRemove(s.ShortCode, out _);
                _redirectEvents.TryRemove(id, out _);
                _auditEvents.TryRemove(id, out _);
            }
        }

        public void AddRedirectEvent(Models.RedirectEvent e)
        {
            var bag = _redirectEvents.GetOrAdd(e.ShortUrlId, _ => new System.Collections.Concurrent.ConcurrentBag<Models.RedirectEvent>());
            bag.Add(e);
        }

        public System.Collections.Generic.IEnumerable<Models.RedirectEvent> ListRedirectEvents(Guid shortUrlId, DateTimeOffset since)
        {
            if (_redirectEvents.TryGetValue(shortUrlId, out var bag))
            {
                return bag.Where(e => e.Timestamp >= since).OrderBy(e => e.Timestamp).ToList();
            }
            return System.Array.Empty<Models.RedirectEvent>();
        }

        public void AddAuditEvent(Models.AuditEvent e)
        {
            var bag = _auditEvents.GetOrAdd(e.TargetEntityId, _ => new System.Collections.Concurrent.ConcurrentBag<Models.AuditEvent>());
            bag.Add(e);
        }

        public System.Collections.Generic.IEnumerable<Models.AuditEvent> ListAuditEvents(string targetEntityType, Guid targetEntityId, DateTimeOffset since)
        {
            if (_auditEvents.TryGetValue(targetEntityId, out var bag))
            {
                return bag.Where(a => a.Timestamp >= since && string.Equals(a.TargetEntityType, targetEntityType, StringComparison.OrdinalIgnoreCase)).OrderBy(a => a.Timestamp).ToList();
            }
            return System.Array.Empty<Models.AuditEvent>();
        }

        // Rate tier operations
        public Models.RateTierEntity CreateRateTier(Models.RateTierEntity r)
        {
            _rateTiers[r.Id] = r;
            return r;
        }

        public Models.RateTierEntity? GetRateTier(Guid id) => _rateTiers.TryGetValue(id, out var r) ? r : null;
        public Models.RateTierEntity? GetRateTierByName(string name) => _rateTiers.Values.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        public System.Collections.Generic.IEnumerable<Models.RateTierEntity> ListRateTiers() => _rateTiers.Values;
        public void UpdateRateTier(Models.RateTierEntity r) => _rateTiers[r.Id] = r;
        public void DeleteRateTier(Guid id) => _rateTiers.TryRemove(id, out _);

        public void IncrementRedirect(ShortUrl s)
        {
            s.RedirectCount++;
            s.UpdatedAt = System.DateTimeOffset.UtcNow;
            _urls[s.Id] = s;

            // record a redirect event for basic analytics
            var bag = _redirectEvents.GetOrAdd(s.Id, _ => new System.Collections.Concurrent.ConcurrentBag<Models.RedirectEvent>());
            bag.Add(new Models.RedirectEvent { ShortUrlId = s.Id, Timestamp = DateTimeOffset.UtcNow });
        }
    }
}
