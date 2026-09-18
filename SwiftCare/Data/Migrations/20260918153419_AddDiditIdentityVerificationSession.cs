using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiditIdentityVerificationSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderSessionId",
                table: "VolunteerIdentityVerifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationUrl",
                table: "VolunteerIdentityVerifications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerIdentityVerifications_ProviderSessionId",
                table: "VolunteerIdentityVerifications",
                column: "ProviderSessionId",
                unique: true,
                filter: "[ProviderSessionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VolunteerIdentityVerifications_ProviderSessionId",
                table: "VolunteerIdentityVerifications");

            migrationBuilder.DropColumn(
                name: "ProviderSessionId",
                table: "VolunteerIdentityVerifications");

            migrationBuilder.DropColumn(
                name: "VerificationUrl",
                table: "VolunteerIdentityVerifications");
        }
    }
}
