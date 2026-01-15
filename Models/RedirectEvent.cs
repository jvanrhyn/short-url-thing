using System;

namespace test_ins.Models
{
    public class RedirectEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShortUrlId { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? RemoteIp { get; set; }
        public string? UserAgent { get; set; }
    }
}
