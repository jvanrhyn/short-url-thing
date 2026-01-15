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

        // Redirect events for analytics
        void AddRedirectEvent(Models.RedirectEvent e);
        System.Collections.Generic.IEnumerable<Models.RedirectEvent> ListRedirectEvents(Guid shortUrlId, DateTimeOffset since);

        // Audit events for mutation logging
        void AddAuditEvent(Models.AuditEvent e);
        System.Collections.Generic.IEnumerable<Models.AuditEvent> ListAuditEvents(string targetEntityType, Guid targetEntityId, DateTimeOffset since);
    }
}
