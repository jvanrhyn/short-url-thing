using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace test_ins.Migrations
{
    public partial class AddRateTiers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RateTierEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestsPerMinute = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateTierEntities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RateTierEntities_Name",
                table: "RateTierEntities",
                column: "Name",
                unique: true);

            // seed default tiers
            migrationBuilder.InsertData(
                table: "RateTierEntities",
                columns: new[] { "Id", "Name", "RequestsPerMinute", "Description" },
                values: new object[] { Guid.NewGuid(), "free", 60, "Free tier" });

            migrationBuilder.InsertData(
                table: "RateTierEntities",
                columns: new[] { "Id", "Name", "RequestsPerMinute", "Description" },
                values: new object[] { Guid.NewGuid(), "team", 300, "Team tier" });

            migrationBuilder.InsertData(
                table: "RateTierEntities",
                columns: new[] { "Id", "Name", "RequestsPerMinute", "Description" },
                values: new object[] { Guid.NewGuid(), "enterprise", 2000, "Enterprise tier" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RateTierEntities");
        }
    }
}
