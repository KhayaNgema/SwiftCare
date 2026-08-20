using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransReq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    VehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.VehicleId);
                });

            migrationBuilder.CreateTable(
                name: "TransportRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedToId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedForId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedForDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedForTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PickUpLatitude = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickUpLongitude = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentLatitude = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentLongitude = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DestinationLatitude = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DestinationLongitude = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_TransportRequests_AspNetUsers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_TransportRequests_AspNetUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_TransportRequests_AspNetUsers_RequestedForId",
                        column: x => x.RequestedForId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_TransportRequests_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "VehicleId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransportRequests_AssignedToId",
                table: "TransportRequests",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportRequests_RequestedById",
                table: "TransportRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_TransportRequests_RequestedForId",
                table: "TransportRequests",
                column: "RequestedForId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportRequests_VehicleId",
                table: "TransportRequests",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransportRequests");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
