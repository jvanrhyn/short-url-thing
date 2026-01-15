using System;
using System.Text.RegularExpressions;
using test_ins.DTOs;
using test_ins.Models;
using test_ins.Repositories;

namespace test_ins.Services
{
    public interface IUrlService
    {
        ShortUrl Create(User owner, ShortUrlCreate req);
        ShortUrl? GetById(Guid id);
        ShortUrl? GetByShortCode(string code);
        void IncrementRedirect(ShortUrl s);
        void Update(ShortUrl s, ShortUrlUpdate update);
        void Delete(Guid id);
    }

    public class UrlService : IUrlService
    {
        private readonly Repositories.IRepo _repo;
        private readonly Microsoft.Extensions.Logging.ILogger<UrlService> _logger;
        private static readonly Regex AliasRegex = new("^[a-zA-Z0-9_-]{4,40}$");

        public UrlService(Repositories.IRepo repo, Microsoft.Extensions.Logging.ILogger<UrlService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        private string GenerateShortCode()
        {
            // simple base62-ish generator for scaffold
            var guid = Guid.NewGuid().ToString("N");
            return guid.Substring(0, 8);
        }

        public ShortUrl Create(User owner, ShortUrlCreate req)
        {
            if (!Uri.TryCreate(req.Destination, UriKind.Absolute, out var _))
            {
                _logger.LogWarning("Invalid destination URL provided by user {OwnerId}", owner.UserId);
                throw new ArgumentException("Invalid destination url");
            }

            var shortCode = req.CustomAlias;
            if (!string.IsNullOrWhiteSpace(shortCode))
            {
                if (!AliasRegex.IsMatch(shortCode))
                {
                    _logger.LogWarning("Invalid custom alias '{Alias}' provided by user {OwnerId}", shortCode, owner.UserId);
                    throw new ArgumentException("Invalid custom alias");
                }

                if (_repo.GetByShortCode(shortCode) != null)
                {
                    _logger.LogWarning("Attempt to use already-existing alias '{Alias}' by user {OwnerId}", shortCode, owner.UserId);
                    throw new ArgumentException("Alias already in use");
                }
            }
            else
            {
                shortCode = GenerateShortCode();
            }

            var s = new ShortUrl
            {
                ShortCode = shortCode!,
                Destination = req.Destination,
                OwnerUserId = owner.UserId,
                ExpiresAt = req.ExpiresAt,
                CustomAlias = req.CustomAlias
            };

            var created = _repo.CreateShortUrl(s);
            _logger.LogInformation("Created short URL {ShortCode} (id={Id}) for owner {OwnerId}", created.ShortCode, created.Id, owner.UserId);

            // audit
            _repo.AddAuditEvent(new Models.AuditEvent
            {
                ActorUserId = owner.UserId,
                Action = "shorturl:create",
                TargetEntityType = "ShortUrl",
                TargetEntityId = created.Id,
                Details = $"{{\"destination\":\"{created.Destination}\"}}"
            });

            return created;
        }

        public ShortUrl? GetById(Guid id) => _repo.GetShortUrl(id);
        public ShortUrl? GetByShortCode(string code) => _repo.GetByShortCode(code);
        public void IncrementRedirect(ShortUrl s)
        {
            _repo.IncrementRedirect(s);
            _repo.AddRedirectEvent(new Models.RedirectEvent { ShortUrlId = s.Id, Timestamp = DateTimeOffset.UtcNow });
        }
        public void Update(ShortUrl s, ShortUrlUpdate update)
        {
            if (update.Status.HasValue)
                s.Status = update.Status.Value;
            if (update.ExpiresAt.HasValue)
                s.ExpiresAt = update.ExpiresAt.Value;
            _repo.UpdateShortUrl(s);

            // audit
            _repo.AddAuditEvent(new Models.AuditEvent
            {
                ActorUserId = s.OwnerUserId,
                Action = "shorturl:update",
                TargetEntityType = "ShortUrl",
                TargetEntityId = s.Id,
                Details = $"{{\"status\":\"{s.Status}\"}}"
            });
        }

        public void Delete(Guid id)
        {
            var s = _repo.GetShortUrl(id);
            if (s != null)
            {
                _repo.DeleteShortUrl(id);
                _repo.AddAuditEvent(new Models.AuditEvent
                {
                    ActorUserId = s.OwnerUserId,
                    Action = "shorturl:delete",
                    TargetEntityType = "ShortUrl",
                    TargetEntityId = s.Id,
                    Details = null
                });
            }
        }
    }
}
