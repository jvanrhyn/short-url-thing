using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using test_ins.Persistence;

#nullable disable

namespace test_ins.Migrations
{
    [DbContext(typeof(ShortenerDbContext))]
    partial class ShortenerDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("test_ins.Models.User", b =>
            {
                b.Property<Guid>("UserId").ValueGeneratedOnAdd();
                b.Property<string>("ApiKey").IsRequired();
                b.Property<string>("Email").IsRequired();
                b.Property<int>("Status");
                b.HasKey("UserId");
                b.HasIndex("ApiKey");
                b.ToTable("Users");
            });

            modelBuilder.Entity("test_ins.Models.ShortUrl", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedOnAdd();
                b.Property<DateTimeOffset>("CreatedAt");
                b.Property<string>("CustomAlias");
                b.Property<string>("Destination").IsRequired();
                b.Property<DateTimeOffset?>("ExpiresAt");
                b.Property<DateTimeOffset?>("UpdatedAt");
                b.Property<Guid>("OwnerUserId");
                b.Property<long>("RedirectCount");
                b.Property<int>("Status");
                b.Property<string>("ShortCode").IsRequired();
                b.HasKey("Id");
                b.HasIndex("ShortCode").IsUnique();
                b.ToTable("ShortUrls");
            });
        }
    }
}
