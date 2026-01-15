namespace test_ins.Models
{
    public class RateTier
    {
        public string Name { get; set; } = string.Empty;
        public int RequestsPerMinute { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
