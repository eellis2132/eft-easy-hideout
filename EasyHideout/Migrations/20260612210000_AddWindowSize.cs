using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyHideout.Migrations
{
    /// <inheritdoc />
    public partial class AddWindowSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "WindowWidth",
                table: "AppSettings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WindowHeight",
                table: "AppSettings",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WindowWidth",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "WindowHeight",
                table: "AppSettings");
        }
    }
}
