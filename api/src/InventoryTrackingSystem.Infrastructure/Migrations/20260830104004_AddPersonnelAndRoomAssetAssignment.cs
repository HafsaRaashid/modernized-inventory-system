using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryTrackingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonnelAndRoomAssetAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Personnel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomAssetAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: true),
                    PersonnelId = table.Column<int>(type: "int", nullable: true),
                    AssetId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomAssetAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomAssetAssignments_Personnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "Personnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoomAssetAssignments_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssetAssignments_PersonnelId",
                table: "RoomAssetAssignments",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssetAssignments_RoomId",
                table: "RoomAssetAssignments",
                column: "RoomId");

            // Local dev/test placeholder rows only (CQ-006/CQ-012: real
            // Personnel provisioning happens outside this application) —
            // without these, the Room Assignment screen's personnel
            // selector has nothing to list.
            migrationBuilder.InsertData(
                table: "Personnel",
                columns: new[] { "Id", "FirstName", "LastName" },
                values: new object[,]
                {
                    { 1, "Ahmet", "Yılmaz" },
                    { 2, "Ayşe", "Kaya" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomAssetAssignments");

            migrationBuilder.DropTable(
                name: "Personnel");
        }
    }
}
