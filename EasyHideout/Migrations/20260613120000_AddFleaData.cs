using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyHideout.Migrations
{
    public partial class AddFleaData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApiRefreshIntervalMinutes",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<string>(
                name: "ApiRefreshMode",
                table: "AppSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<int>(
                name: "AvgPrice",
                table: "ItemRequirements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FoundInRaid",
                table: "ItemRequirements",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MinLevelForFlea",
                table: "ItemRequirements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ApiRefreshIntervalMinutes", table: "AppSettings");
            migrationBuilder.DropColumn(name: "ApiRefreshMode", table: "AppSettings");
            migrationBuilder.DropColumn(name: "AvgPrice", table: "ItemRequirements");
            migrationBuilder.DropColumn(name: "FoundInRaid", table: "ItemRequirements");
            migrationBuilder.DropColumn(name: "MinLevelForFlea", table: "ItemRequirements");
        }
    }
}
