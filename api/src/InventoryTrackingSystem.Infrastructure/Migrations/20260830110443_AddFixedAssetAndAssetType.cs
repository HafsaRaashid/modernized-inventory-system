using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryTrackingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedAssetAndAssetType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FixedAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssetTypeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixedAssets_AssetTypes_AssetTypeId",
                        column: x => x.AssetTypeId,
                        principalTable: "AssetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_AssetTypeId",
                table: "FixedAssets",
                column: "AssetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_Name",
                table: "FixedAssets",
                column: "Name",
                unique: true);

            // Local dev/test placeholder rows only (real AssetType
            // provisioning happens outside this application) — without
            // these, the Fixed Asset screens' asset type picker has
            // nothing to list.
            migrationBuilder.InsertData(
                table: "AssetTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Elektronik" },
                    { 2, "Mobilya" },
                    { 3, "Ofis Malzemesi" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixedAssets");

            migrationBuilder.DropTable(
                name: "AssetTypes");
        }
    }
}
