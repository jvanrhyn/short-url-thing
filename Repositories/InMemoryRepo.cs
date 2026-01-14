using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using test_ins.Models;

namespace test_ins.Repositories
{
    public class InMemoryRepo : IRepo
    {
        private readonly ConcurrentDictionary<Guid, User> _users = new();
        private readonly ConcurrentDictionary<Guid, ShortUrl> _urls = new();
        private readonly ConcurrentDictionary<string, Guid> _shortCodeIndex = new(StringComparer.OrdinalIgnoreCase);

        public InMemoryRepo()
        {
            // seed a default user for local dev
            var user = new User
            {
                Email = "dev@example.local",
                ApiKey = "dev-api-key-123",
            };
            _users[user.UserId] = user;
        }

        // Users
        public User? GetUserByApiKey(string apiKey) => _users.Values.FirstOrDefault(u => u.ApiKey == apiKey);
        public User? GetUser(Guid id) => _users.TryGetValue(id, out var u) ? u : null;
        public IEnumerable<User> ListUsers() => _users.Values;
        public void AddUser(User user) => _users[user.UserId] = user;

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
            }
        }

        public void IncrementRedirect(ShortUrl s)
        {
            s.RedirectCount++;
            s.UpdatedAt = System.DateTimeOffset.UtcNow;
            _urls[s.Id] = s;
        }
    }
}
