using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerContractSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequiredSkillsSnapshot",
                table: "VolunteerContracts",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequirementsSnapshot",
                table: "VolunteerContracts",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibilitiesSnapshot",
                table: "VolunteerContracts",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredSkillsSnapshot",
                table: "VolunteerContracts");

            migrationBuilder.DropColumn(
                name: "RequirementsSnapshot",
                table: "VolunteerContracts");

            migrationBuilder.DropColumn(
                name: "ResponsibilitiesSnapshot",
                table: "VolunteerContracts");
        }
    }
}
