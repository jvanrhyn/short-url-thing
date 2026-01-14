using System;
using System.ComponentModel.DataAnnotations;
using test_ins.Models;

namespace test_ins.DTOs
{
    public class ShortUrlUpdate
    {
        public UrlStatus? Status { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
