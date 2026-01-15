using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace test_ins.Migrations
{
    public partial class AddUserRateLimit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RateLimitRpm",
                table: "Users",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RateLimitRpm",
                table: "Users");
        }
    }
}
