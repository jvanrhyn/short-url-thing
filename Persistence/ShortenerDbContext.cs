using Microsoft.EntityFrameworkCore;
using test_ins.Models;

namespace test_ins.Persistence
{
    public class ShortenerDbContext : DbContext
    {
        public ShortenerDbContext(DbContextOptions<ShortenerDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ShortUrl> ShortUrls { get; set; } = null!;

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

            base.OnModelCreating(modelBuilder);
        }
    }
}
