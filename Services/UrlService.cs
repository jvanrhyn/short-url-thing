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
        private static readonly Regex AliasRegex = new("^[a-zA-Z0-9_-]{4,40}$");

        public UrlService(Repositories.IRepo repo)
        {
            _repo = repo;
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
                throw new ArgumentException("Invalid destination url");

            var shortCode = req.CustomAlias;
            if (!string.IsNullOrWhiteSpace(shortCode))
            {
                if (!AliasRegex.IsMatch(shortCode))
                    throw new ArgumentException("Invalid custom alias");

                if (_repo.GetByShortCode(shortCode) != null)
                    throw new ArgumentException("Alias already in use");
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

            return _repo.CreateShortUrl(s);
        }

        public ShortUrl? GetById(Guid id) => _repo.GetShortUrl(id);
        public ShortUrl? GetByShortCode(string code) => _repo.GetByShortCode(code);
        public void IncrementRedirect(ShortUrl s) => _repo.IncrementRedirect(s);
        public void Update(ShortUrl s, ShortUrlUpdate update)
        {
            if (update.Status.HasValue)
                s.Status = update.Status.Value;
            if (update.ExpiresAt.HasValue)
                s.ExpiresAt = update.ExpiresAt.Value;
            _repo.UpdateShortUrl(s);
        }
        public void Delete(Guid id) => _repo.DeleteShortUrl(id);
    }
}
