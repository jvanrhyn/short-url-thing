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
                _db.ShortUrls.Remove(s);
                _db.SaveChanges();
            }
        }

        public void IncrementRedirect(ShortUrl s)
        {
            s.RedirectCount++;
            s.UpdatedAt = DateTimeOffset.UtcNow;
            _db.ShortUrls.Update(s);
            _db.SaveChanges();
        }
    }
}
