using System;

namespace test_ins.Models
{
    public class ShortUrl
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ShortCode { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public Guid OwnerUserId { get; set; }
        public UrlStatus Status { get; set; } = UrlStatus.Active;
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? CustomAlias { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public long RedirectCount { get; set; }
    }
}
