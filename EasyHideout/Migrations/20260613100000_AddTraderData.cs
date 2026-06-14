using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyHideout.Migrations
{
    /// <inheritdoc />
    public partial class AddTraderData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CharacterLevel",
                table: "Profiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "TraderLoyaltyLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TraderId = table.Column<string>(type: "TEXT", nullable: false),
                    TraderName = table.Column<string>(type: "TEXT", nullable: false),
                    LoyaltyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredPlayerLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraderLoyaltyLevels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TraderLoyaltyLevels_TraderId_LoyaltyLevel",
                table: "TraderLoyaltyLevels",
                columns: new[] { "TraderId", "LoyaltyLevel" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "TraderRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HideoutLevelId = table.Column<int>(type: "INTEGER", nullable: false),
                    TraderId = table.Column<string>(type: "TEXT", nullable: false),
                    TraderName = table.Column<string>(type: "TEXT", nullable: false),
                    RequiredLoyaltyLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraderRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraderRequirements_HideoutLevels_HideoutLevelId",
                        column: x => x.HideoutLevelId,
                        principalTable: "HideoutLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TraderRequirements_HideoutLevelId",
                table: "TraderRequirements",
                column: "HideoutLevelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TraderRequirements");
            migrationBuilder.DropTable(name: "TraderLoyaltyLevels");
            migrationBuilder.DropColumn(name: "CharacterLevel", table: "Profiles");
        }
    }
}
