using System;

namespace test_ins.Models
{
    public class User
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public UserStatus Status { get; set; } = UserStatus.Active;
        public string ApiKey { get; set; } = string.Empty; // dev-only: in-memory
        // Per-user rate limit (requests per minute). Null => use default tier
        public int? RateLimitRpm { get; set; }
        // Admin flag - indicates user can manage tiers and view all users
        public bool IsAdmin { get; set; } = false;
    }
}
