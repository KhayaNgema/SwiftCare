using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyCare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyCare",
                columns: table => new
                {
                    CareId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NurseId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CareNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResidentId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyCare", x => x.CareId);
                    table.ForeignKey(
                        name: "FK_DailyCare_AspNetUsers_NurseId",
                        column: x => x.NurseId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_DailyCare_AspNetUsers_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "DailyMaintenance",
                columns: table => new
                {
                    MaintenanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenancePersonId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaintenanceStatus = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    MaintenanceRequestRequestId = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMaintenance", x => x.MaintenanceId);
                    table.ForeignKey(
                        name: "FK_DailyMaintenance_AspNetUsers_MaintenancePersonId",
                        column: x => x.MaintenancePersonId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_DailyMaintenance_MaintenanceRequests_MaintenanceRequestRequestId",
                        column: x => x.MaintenanceRequestRequestId,
                        principalTable: "MaintenanceRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyCare_NurseId",
                table: "DailyCare",
                column: "NurseId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyCare_ResidentId",
                table: "DailyCare",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMaintenance_MaintenancePersonId",
                table: "DailyMaintenance",
                column: "MaintenancePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMaintenance_MaintenanceRequestRequestId",
                table: "DailyMaintenance",
                column: "MaintenanceRequestRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyCare");

            migrationBuilder.DropTable(
                name: "DailyMaintenance");
        }
    }
}
