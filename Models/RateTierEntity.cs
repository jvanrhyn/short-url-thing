using System;

namespace test_ins.Models
{
    public class RateTierEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int RequestsPerMinute { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
