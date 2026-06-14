using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyHideout.Migrations
{
    /// <inheritdoc />
    public partial class AddWindowPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "WindowLeft",
                table: "AppSettings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WindowTop",
                table: "AppSettings",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WindowLeft",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "WindowTop",
                table: "AppSettings");
        }
    }
}
