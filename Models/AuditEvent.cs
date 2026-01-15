using System;

namespace test_ins.Models
{
    public class AuditEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ActorUserId { get; set; } // user performing the action, null for system
        public string Action { get; set; } = string.Empty; // e.g., "shorturl:create", "shorturl:update"
        public string TargetEntityType { get; set; } = string.Empty; // e.g., "ShortUrl", "User"
        public Guid TargetEntityId { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? Details { get; set; } // optional JSON or human-readable detail
    }
}
