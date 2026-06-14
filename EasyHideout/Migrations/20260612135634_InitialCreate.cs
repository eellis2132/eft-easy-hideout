using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyHideout.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HideoutStations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TarkovStationId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    MaxLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HideoutStations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HideoutLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HideoutLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HideoutLevels_HideoutStations_StationId",
                        column: x => x.StationId,
                        principalTable: "HideoutStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActiveProfileId = table.Column<int>(type: "INTEGER", nullable: true),
                    DetailPanelPosition = table.Column<string>(type: "TEXT", nullable: false),
                    LastApiRefresh = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSettings_Profiles_ActiveProfileId",
                        column: x => x.ActiveProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FocusNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    StationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FocusNodes_HideoutStations_StationId",
                        column: x => x.StationId,
                        principalTable: "HideoutStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FocusNodes_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IgnoredItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    TarkovItemId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IgnoredItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IgnoredItems_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportantItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    TarkovItemId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportantItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportantItems_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    TarkovItemId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", nullable: false),
                    QuantityOwned = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCounts_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileStationLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    StationId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileStationLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileStationLevels_HideoutStations_StationId",
                        column: x => x.StationId,
                        principalTable: "HideoutStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileStationLevels_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HideoutLevelId = table.Column<int>(type: "INTEGER", nullable: false),
                    TarkovItemId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    IconUrl = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemRequirements_HideoutLevels_HideoutLevelId",
                        column: x => x.HideoutLevelId,
                        principalTable: "HideoutLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StationDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HideoutLevelId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredStationId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StationDependencies_HideoutLevels_HideoutLevelId",
                        column: x => x.HideoutLevelId,
                        principalTable: "HideoutLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StationDependencies_HideoutStations_RequiredStationId",
                        column: x => x.RequiredStationId,
                        principalTable: "HideoutStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_ActiveProfileId",
                table: "AppSettings",
                column: "ActiveProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FocusNodes_ProfileId",
                table: "FocusNodes",
                column: "ProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FocusNodes_StationId",
                table: "FocusNodes",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_HideoutLevels_StationId",
                table: "HideoutLevels",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_HideoutStations_TarkovStationId",
                table: "HideoutStations",
                column: "TarkovStationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IgnoredItems_ProfileId_TarkovItemId",
                table: "IgnoredItems",
                columns: new[] { "ProfileId", "TarkovItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportantItems_ProfileId_TarkovItemId",
                table: "ImportantItems",
                columns: new[] { "ProfileId", "TarkovItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCounts_ProfileId_TarkovItemId",
                table: "ItemCounts",
                columns: new[] { "ProfileId", "TarkovItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequirements_HideoutLevelId",
                table: "ItemRequirements",
                column: "HideoutLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileStationLevels_ProfileId_StationId",
                table: "ProfileStationLevels",
                columns: new[] { "ProfileId", "StationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileStationLevels_StationId",
                table: "ProfileStationLevels",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_StationDependencies_HideoutLevelId",
                table: "StationDependencies",
                column: "HideoutLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_StationDependencies_RequiredStationId",
                table: "StationDependencies",
                column: "RequiredStationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "FocusNodes");

            migrationBuilder.DropTable(
                name: "IgnoredItems");

            migrationBuilder.DropTable(
                name: "ImportantItems");

            migrationBuilder.DropTable(
                name: "ItemCounts");

            migrationBuilder.DropTable(
                name: "ItemRequirements");

            migrationBuilder.DropTable(
                name: "ProfileStationLevels");

            migrationBuilder.DropTable(
                name: "StationDependencies");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "HideoutLevels");

            migrationBuilder.DropTable(
                name: "HideoutStations");
        }
    }
}
