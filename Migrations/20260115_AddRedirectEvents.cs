using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace test_ins.Migrations
{
    public partial class AddRedirectEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RedirectEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShortUrlId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RemoteIp = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedirectEvents", x => x.Id);
                    table.ForeignKey("FK_RedirectEvents_ShortUrls_ShortUrlId", x => x.ShortUrlId, "ShortUrls", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RedirectEvents_ShortUrlId_Timestamp",
                table: "RedirectEvents",
                columns: new[] { "ShortUrlId", "Timestamp" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RedirectEvents");
        }
    }
}
