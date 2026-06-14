using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyHideout.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityShowAndNameFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriorityL1Show",
                table: "AppSettings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityL2Show",
                table: "AppSettings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemNameDisplay",
                table: "AppSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "Both");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PriorityL1Show", table: "AppSettings");
            migrationBuilder.DropColumn(name: "PriorityL2Show", table: "AppSettings");
            migrationBuilder.DropColumn(name: "ItemNameDisplay", table: "AppSettings");
        }
    }
}
