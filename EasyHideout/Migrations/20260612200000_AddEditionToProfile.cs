using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyHideout.Migrations
{
    /// <inheritdoc />
    public partial class AddEditionToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Edition",
                table: "Profiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "Standard");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Edition",
                table: "Profiles");
        }
    }
}
