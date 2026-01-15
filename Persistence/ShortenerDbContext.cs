using Microsoft.EntityFrameworkCore;
using test_ins.Models;

namespace test_ins.Persistence
{
    public class ShortenerDbContext : DbContext
    {
        public ShortenerDbContext(DbContextOptions<ShortenerDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ShortUrl> ShortUrls { get; set; } = null!;
        public DbSet<RedirectEvent> RedirectEvents { get; set; } = null!;
        public DbSet<AuditEvent> AuditEvents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.UserId);
                b.HasIndex(u => u.ApiKey).IsUnique(false);
            });

            modelBuilder.Entity<ShortUrl>(b =>
            {
                b.HasKey(s => s.Id);
                b.HasIndex(s => s.ShortCode).IsUnique();
                b.Property(s => s.ShortCode).IsRequired();
                b.Property(s => s.Destination).IsRequired();
            });

            modelBuilder.Entity<RedirectEvent>(b =>
            {
                b.HasKey(e => e.Id);
                b.HasIndex(e => new { e.ShortUrlId, e.Timestamp });
                b.Property(e => e.Timestamp).IsRequired();
            });

            modelBuilder.Entity<AuditEvent>(b =>
            {
                b.HasKey(a => a.Id);
                b.HasIndex(a => new { a.TargetEntityType, a.TargetEntityId });
                b.Property(a => a.Timestamp).IsRequired();
                b.Property(a => a.Action).IsRequired().HasMaxLength(128);
                b.Property(a => a.TargetEntityType).IsRequired().HasMaxLength(64);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
