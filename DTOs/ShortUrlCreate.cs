using System;
using System.ComponentModel.DataAnnotations;

namespace test_ins.DTOs
{
    public class ShortUrlCreate
    {
        [Required]
        [Url]
        public string Destination { get; set; } = string.Empty;

        public string? CustomAlias { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
