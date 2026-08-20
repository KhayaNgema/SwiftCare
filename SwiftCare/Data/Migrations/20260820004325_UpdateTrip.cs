using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransportRequests_AspNetUsers_AssignedToId",
                table: "TransportRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportRequests_AspNetUsers_RequestedById",
                table: "TransportRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportRequests_AspNetUsers_RequestedForId",
                table: "TransportRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportRequests_Vehicles_VehicleId",
                table: "TransportRequests");

            migrationBuilder.RenameColumn(
                name: "CurrentLongitude",
                table: "TransportRequests",
                newName: "PickUpAddress");

            migrationBuilder.RenameColumn(
                name: "CurrentLatitude",
                table: "TransportRequests",
                newName: "DestinationAddress");

            migrationBuilder.AlterColumn<double>(
                name: "PickUpLongitude",
                table: "TransportRequests",
                type: "float",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<double>(
                name: "PickUpLatitude",
                table: "TransportRequests",
                type: "float",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<double>(
                name: "DestinationLongitude",
                table: "TransportRequests",
                type: "float",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<double>(
                name: "DestinationLatitude",
                table: "TransportRequests",
                type: "float",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedToId",
                table: "TransportRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "TravelDistance",
                table: "TransportRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TravelTime",
                table: "TransportRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Trip",
                columns: table => new
                {
                    TripId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "float", nullable: true),
                    CurrentLongitude = table.Column<double>(type: "float", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trip", x => x.TripId);
                    table.ForeignKey(
                        name: "FK_Trip_TransportRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "TransportRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trip_RequestId",
                table: "Trip",
                column: "RequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportRequests_AspNetUsers_AssignedToId",
                table: "TransportRequests",
                column: "AssignedToId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportRequests_AspNetUsers_RequestedById",
                table: "TransportRequests",
                column: "RequestedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportRequests_AspNetUsers_RequestedForId",
                table: "TransportRequests",
                column: "RequestedForId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportRequests_Vehicles_VehicleId",
                table: "TransportRequests",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "VehicleId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransportRequests_AspNetUsers_AssignedToId",
                table: "TransportRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportRequests_AspNetUsers_RequestedById",
                table: "TransportRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportRequests_AspNetUsers_RequestedForId",
                table: "TransportRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportRequests_Vehicles_VehicleId",
                table: "TransportRequests");

            migrationBuilder.DropTable(
                name: "Trip");

            migrationBuilder.DropColumn(
                name: "TravelDistance",
                table: "TransportRequests");

            migrationBuilder.DropColumn(
                name: "TravelTime",
                table: "TransportRequests");

            migrationBuilder.RenameColumn(
                name: "PickUpAddress",
                table: "TransportRequests",
                newName: "CurrentLongitude");

            migrationBuilder.RenameColumn(
                name: "DestinationAddress",
                table: "TransportRequests",
                newName: "CurrentLatitude");

            migrationBuilder.AlterColumn<string>(
                name: "PickUpLongitude",
                table: "TransportRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PickUpLatitude",
                table: "TransportRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DestinationLongitude",
                table: "TransportRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DestinationLatitude",
                table: "TransportRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AssignedToId",
                table: "TransportRequests",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportRequests_AspNetUsers_AssignedToId",
                table: "TransportRequests",
                column: "AssignedToId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportRequests_AspNetUsers_RequestedById",
                table: "TransportRequests",
                column: "RequestedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportRequests_AspNetUsers_RequestedForId",
                table: "TransportRequests",
                column: "RequestedForId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportRequests_Vehicles_VehicleId",
                table: "TransportRequests",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "VehicleId");
        }
    }
}
