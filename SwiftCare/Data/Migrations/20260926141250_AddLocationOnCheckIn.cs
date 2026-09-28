using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationOnCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CheckInLatitude",
                table: "VolunteerAttendances",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CheckInLocationAccuracy",
                table: "VolunteerAttendances",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CheckInLongitude",
                table: "VolunteerAttendances",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInLatitude",
                table: "VolunteerAttendances");

            migrationBuilder.DropColumn(
                name: "CheckInLocationAccuracy",
                table: "VolunteerAttendances");

            migrationBuilder.DropColumn(
                name: "CheckInLongitude",
                table: "VolunteerAttendances");
        }
    }
}
