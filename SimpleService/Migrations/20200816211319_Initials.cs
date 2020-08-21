using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimpleService.Migrations
{
    public partial class Initials : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    IsFixIncome = table.Column<bool>(nullable: false),
                    IsConvertible = table.Column<bool>(nullable: false),
                    IsSwap = table.Column<bool>(nullable: false),
                    IsCash = table.Column<bool>(nullable: false),
                    IsFuture = table.Column<bool>(nullable: false),
                    TimeStamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assets");
        }
    }
}
