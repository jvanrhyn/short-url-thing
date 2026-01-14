using System;
using System.Collections.Generic;
using test_ins.Models;

namespace test_ins.Repositories
{
    public interface IRepo
    {
        // Users
        User? GetUserByApiKey(string apiKey);
        User? GetUser(Guid id);
        IEnumerable<User> ListUsers();
        void AddUser(User user);

        // ShortUrls
        ShortUrl CreateShortUrl(ShortUrl s);
        ShortUrl? GetShortUrl(Guid id);
        ShortUrl? GetByShortCode(string shortCode);
        IEnumerable<ShortUrl> ListByOwner(Guid ownerId);
        void UpdateShortUrl(ShortUrl s);
        void DeleteShortUrl(Guid id);
        void IncrementRedirect(ShortUrl s);
    }
}
