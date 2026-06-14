using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyHideout.Migrations
{
    public partial class AddItemPriceSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemPriceSnapshots",
                columns: table => new
                {
                    TarkovItemId = table.Column<string>(type: "TEXT", nullable: false),
                    PreviousAvgPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    SnapshotAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPriceSnapshots", x => x.TarkovItemId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ItemPriceSnapshots");
        }
    }
}
