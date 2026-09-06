using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVityThing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOpportunities_Cities_CityId",
                table: "VolunteerOpportunities");

            migrationBuilder.AddColumn<string>(
                name: "CityName",
                table: "VolunteerOpportunities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GeoNameId",
                table: "VolunteerOpportunities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "VolunteerOpportunities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOpportunities_Cities_CityId",
                table: "VolunteerOpportunities",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "CityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOpportunities_Cities_CityId",
                table: "VolunteerOpportunities");

            migrationBuilder.DropColumn(
                name: "CityName",
                table: "VolunteerOpportunities");

            migrationBuilder.DropColumn(
                name: "GeoNameId",
                table: "VolunteerOpportunities");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "VolunteerOpportunities");

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOpportunities_Cities_CityId",
                table: "VolunteerOpportunities",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "CityId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
