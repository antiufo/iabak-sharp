using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace IaBak.Server.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchiveItems",
                columns: table => new
                {
                    Identifier = table.Column<string>(nullable: false),
                    TotalSize = table.Column<long>(nullable: false),
                    CurrentRedundancy = table.Column<int>(nullable: false),
                    Priority = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveItems", x => x.Identifier);
                });

            migrationBuilder.CreateTable(
                name: "ItemStorage",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    ItemId = table.Column<string>(nullable: false),
                    DateNotified = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemStorage", x => new { x.ItemId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "RecentSuggestions",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false),
                    ItemId = table.Column<string>(nullable: false),
                    SuggestionDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentSuggestions", x => new { x.ItemId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SecretKey = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    RegistrationDate = table.Column<DateTime>(nullable: false),
                    RegistrationIp = table.Column<string>(nullable: true),
                    LastSync = table.Column<DateTime>(nullable: false),
                    Nickname = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchiveItems");

            migrationBuilder.DropTable(
                name: "ItemStorage");

            migrationBuilder.DropTable(
                name: "RecentSuggestions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
