using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrpss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trip_TransportRequests_RequestId",
                table: "Trip");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trip",
                table: "Trip");

            migrationBuilder.RenameTable(
                name: "Trip",
                newName: "Trips");

            migrationBuilder.RenameIndex(
                name: "IX_Trip_RequestId",
                table: "Trips",
                newName: "IX_Trips_RequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trips",
                table: "Trips",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_TransportRequests_RequestId",
                table: "Trips",
                column: "RequestId",
                principalTable: "TransportRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_TransportRequests_RequestId",
                table: "Trips");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Trips",
                table: "Trips");

            migrationBuilder.RenameTable(
                name: "Trips",
                newName: "Trip");

            migrationBuilder.RenameIndex(
                name: "IX_Trips_RequestId",
                table: "Trip",
                newName: "IX_Trip_RequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trip",
                table: "Trip",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trip_TransportRequests_RequestId",
                table: "Trip",
                column: "RequestId",
                principalTable: "TransportRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
