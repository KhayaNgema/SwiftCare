using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class FaceVerificationInProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VolunteerIdentityVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerOnboardingId = table.Column<int>(type: "int", nullable: false),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IdentityDocumentId = table.Column<int>(type: "int", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderVerificationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProviderPersonId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DocumentVerified = table.Column<bool>(type: "bit", nullable: false),
                    LivenessVerified = table.Column<bool>(type: "bit", nullable: false),
                    FaceMatchVerified = table.Column<bool>(type: "bit", nullable: false),
                    FaceIdentityEnrolled = table.Column<bool>(type: "bit", nullable: false),
                    VerificationStartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerIdentityVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerIdentityVerifications_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerIdentityVerifications_VolunteerOnboardingDocuments_IdentityDocumentId",
                        column: x => x.IdentityDocumentId,
                        principalTable: "VolunteerOnboardingDocuments",
                        principalColumn: "VolunteerOnboardingDocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerIdentityVerifications_VolunteerOnboardings_VolunteerOnboardingId",
                        column: x => x.VolunteerOnboardingId,
                        principalTable: "VolunteerOnboardings",
                        principalColumn: "VolunteerOnboardingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerIdentityVerifications_IdentityDocumentId",
                table: "VolunteerIdentityVerifications",
                column: "IdentityDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerIdentityVerifications_ProviderPersonId",
                table: "VolunteerIdentityVerifications",
                column: "ProviderPersonId",
                unique: true,
                filter: "[ProviderPersonId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerIdentityVerifications_ProviderVerificationId",
                table: "VolunteerIdentityVerifications",
                column: "ProviderVerificationId",
                unique: true,
                filter: "[ProviderVerificationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerIdentityVerifications_VolunteerId",
                table: "VolunteerIdentityVerifications",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerIdentityVerifications_VolunteerOnboardingId",
                table: "VolunteerIdentityVerifications",
                column: "VolunteerOnboardingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerIdentityVerifications");
        }
    }
}
