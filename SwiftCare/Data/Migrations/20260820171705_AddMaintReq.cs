using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintReq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OurEquipment",
                columns: table => new
                {
                    EquipmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BarcodeValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BarcodeImage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OurEquipment", x => x.EquipmentId);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    AssignedToId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LoggedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestStatus = table.Column<int>(type: "int", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EquipmentConditionImageBefore = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EquipmentConditionImageAfter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_AspNetUsers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_AspNetUsers_LoggedById",
                        column: x => x.LoggedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_AspNetUsers_ResolvedById",
                        column: x => x.ResolvedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_OurEquipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "OurEquipment",
                        principalColumn: "EquipmentId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_AssignedToId",
                table: "MaintenanceRequests",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_EquipmentId",
                table: "MaintenanceRequests",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_LoggedById",
                table: "MaintenanceRequests",
                column: "LoggedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_ResolvedById",
                table: "MaintenanceRequests",
                column: "ResolvedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceRequests");

            migrationBuilder.DropTable(
                name: "OurEquipment");
        }
    }
}
