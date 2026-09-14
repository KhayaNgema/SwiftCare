using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_OnboardingVolunteerOnboardingId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OnboardingVolunteerOnboardingId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AgreementCompleted",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "DocumentsCompleted",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "OrientationCompleted",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "ProfileCompleted",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "ScreeningCompleted",
                table: "VolunteerOnboardings");

            migrationBuilder.RenameColumn(
                name: "StartedDate",
                table: "VolunteerOnboardings",
                newName: "VolunteerCreatedDate");

            migrationBuilder.RenameColumn(
                name: "OnboardingVolunteerOnboardingId",
                table: "AspNetUsers",
                newName: "VolunteerOnboardingId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractSubmittedDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "VolunteerOnboardings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CurrentStep",
                table: "VolunteerOnboardings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentsSubmittedDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "VolunteerOnboardings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferAcceptedDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferDeclineReason",
                table: "VolunteerOnboardings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferDeclinedDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferSentDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PoliciesAcceptedDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "VolunteerOnboardings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationStartedDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedByAdminId",
                table: "VolunteerOnboardings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedDate",
                table: "VolunteerOnboardings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VolunteerApplicationId",
                table: "VolunteerOnboardings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnboardingCompletedDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VolunteerApplicationId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VolunteerNumber",
                table: "AspNetUsers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VolunteerOffers",
                columns: table => new
                {
                    VolunteerOfferId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerOnboardingId = table.Column<int>(type: "int", nullable: false),
                    VolunteerApplicationId = table.Column<int>(type: "int", nullable: false),
                    ApplicantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OfferReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PositionTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeclinedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeclineReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AcceptanceTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TokenExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerOffers", x => x.VolunteerOfferId);
                    table.ForeignKey(
                        name: "FK_VolunteerOffers_VolunteerApplications_VolunteerApplicationId",
                        column: x => x.VolunteerApplicationId,
                        principalTable: "VolunteerApplications",
                        principalColumn: "VolunteerApplicationId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_VolunteerOffers_VolunteerOnboardings_VolunteerOnboardingId",
                        column: x => x.VolunteerOnboardingId,
                        principalTable: "VolunteerOnboardings",
                        principalColumn: "VolunteerOnboardingId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_VolunteerOffers_VolunteerOpportunities_VolunteerOpportunityId",
                        column: x => x.VolunteerOpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "VolunteerOpportunityId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerOnboardingDocuments",
                columns: table => new
                {
                    VolunteerOnboardingDocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerOnboardingId = table.Column<int>(type: "int", nullable: false),
                    ApplicantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerOnboardingDocuments", x => x.VolunteerOnboardingDocumentId);
                    table.ForeignKey(
                        name: "FK_VolunteerOnboardingDocuments_VolunteerOnboardings_VolunteerOnboardingId",
                        column: x => x.VolunteerOnboardingId,
                        principalTable: "VolunteerOnboardings",
                        principalColumn: "VolunteerOnboardingId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerOnboardingRequirements",
                columns: table => new
                {
                    VolunteerOnboardingRequirementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerOnboardingId = table.Column<int>(type: "int", nullable: false),
                    RequirementType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerOnboardingRequirements", x => x.VolunteerOnboardingRequirementId);
                    table.ForeignKey(
                        name: "FK_VolunteerOnboardingRequirements_VolunteerOnboardings_VolunteerOnboardingId",
                        column: x => x.VolunteerOnboardingId,
                        principalTable: "VolunteerOnboardings",
                        principalColumn: "VolunteerOnboardingId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerPolicies",
                columns: table => new
                {
                    VolunteerPolicyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DocumentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerPolicies", x => x.VolunteerPolicyId);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerPolicyAcceptances",
                columns: table => new
                {
                    VolunteerPolicyAcceptanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerOnboardingId = table.Column<int>(type: "int", nullable: false),
                    VolunteerPolicyId = table.Column<int>(type: "int", nullable: false),
                    ApplicantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Accepted = table.Column<bool>(type: "bit", nullable: false),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicyVersion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerPolicyAcceptances", x => x.VolunteerPolicyAcceptanceId);
                    table.ForeignKey(
                        name: "FK_VolunteerPolicyAcceptances_VolunteerOnboardings_VolunteerOnboardingId",
                        column: x => x.VolunteerOnboardingId,
                        principalTable: "VolunteerOnboardings",
                        principalColumn: "VolunteerOnboardingId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_VolunteerPolicyAcceptances_VolunteerPolicies_VolunteerPolicyId",
                        column: x => x.VolunteerPolicyId,
                        principalTable: "VolunteerPolicies",
                        principalColumn: "VolunteerPolicyId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardings_VolunteerApplicationId",
                table: "VolunteerOnboardings",
                column: "VolunteerApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_VolunteerOnboardingId",
                table: "AspNetUsers",
                column: "VolunteerOnboardingId",
                unique: true,
                filter: "[VolunteerOnboardingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOffers_VolunteerApplicationId",
                table: "VolunteerOffers",
                column: "VolunteerApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOffers_VolunteerOnboardingId",
                table: "VolunteerOffers",
                column: "VolunteerOnboardingId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOffers_VolunteerOpportunityId",
                table: "VolunteerOffers",
                column: "VolunteerOpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardingDocuments_VolunteerOnboardingId",
                table: "VolunteerOnboardingDocuments",
                column: "VolunteerOnboardingId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardingRequirements_VolunteerOnboardingId",
                table: "VolunteerOnboardingRequirements",
                column: "VolunteerOnboardingId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPolicyAcceptances_VolunteerOnboardingId",
                table: "VolunteerPolicyAcceptances",
                column: "VolunteerOnboardingId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPolicyAcceptances_VolunteerPolicyId",
                table: "VolunteerPolicyAcceptances",
                column: "VolunteerPolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_VolunteerOnboardingId",
                table: "AspNetUsers",
                column: "VolunteerOnboardingId",
                principalTable: "VolunteerOnboardings",
                principalColumn: "VolunteerOnboardingId");

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOnboardings_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOnboardings",
                column: "VolunteerApplicationId",
                principalTable: "VolunteerApplications",
                principalColumn: "VolunteerApplicationId",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_VolunteerOnboardingId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOnboardings_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropTable(
                name: "VolunteerOffers");

            migrationBuilder.DropTable(
                name: "VolunteerOnboardingDocuments");

            migrationBuilder.DropTable(
                name: "VolunteerOnboardingRequirements");

            migrationBuilder.DropTable(
                name: "VolunteerPolicyAcceptances");

            migrationBuilder.DropTable(
                name: "VolunteerPolicies");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerOnboardings_VolunteerApplicationId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_VolunteerOnboardingId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ContractSubmittedDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "CurrentStep",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "DocumentsSubmittedDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "OfferAcceptedDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "OfferDeclineReason",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "OfferDeclinedDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "OfferSentDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "PoliciesAcceptedDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "VerificationStartedDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "VerifiedByAdminId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "VerifiedDate",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "VolunteerApplicationId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropColumn(
                name: "ActivatedDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OnboardingCompletedDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VolunteerApplicationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "VolunteerNumber",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "VolunteerCreatedDate",
                table: "VolunteerOnboardings",
                newName: "StartedDate");

            migrationBuilder.RenameColumn(
                name: "VolunteerOnboardingId",
                table: "AspNetUsers",
                newName: "OnboardingVolunteerOnboardingId");

            migrationBuilder.AddColumn<bool>(
                name: "AgreementCompleted",
                table: "VolunteerOnboardings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DocumentsCompleted",
                table: "VolunteerOnboardings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "VolunteerOnboardings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OrientationCompleted",
                table: "VolunteerOnboardings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProfileCompleted",
                table: "VolunteerOnboardings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScreeningCompleted",
                table: "VolunteerOnboardings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OnboardingVolunteerOnboardingId",
                table: "AspNetUsers",
                column: "OnboardingVolunteerOnboardingId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_OnboardingVolunteerOnboardingId",
                table: "AspNetUsers",
                column: "OnboardingVolunteerOnboardingId",
                principalTable: "VolunteerOnboardings",
                principalColumn: "VolunteerOnboardingId");
        }
    }
}
