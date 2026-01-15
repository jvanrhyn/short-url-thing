namespace test_ins.Middleware
{
    public class RateLimitOptions
    {
        // requests allowed per minute per user
        public int RequestsPerMinute { get; set; } = 60;
        // endpoints excluded from rate limiting (prefix checks)
        public string[] ExcludedPathPrefixes { get; set; } = new[] { "/r/", "/health" };
    }
}
