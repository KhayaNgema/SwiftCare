using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerOnboardingConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_VolunteerOnboardingId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOffers_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOffers_VolunteerOpportunities_VolunteerOpportunityId",
                table: "VolunteerOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOnboardings_AspNetUsers_ApplicantId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOnboardings_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerPolicyAcceptances_VolunteerPolicies_VolunteerPolicyId",
                table: "VolunteerPolicyAcceptances");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerPolicyAcceptances_VolunteerOnboardingId",
                table: "VolunteerPolicyAcceptances");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerOnboardings_VolunteerApplicationId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerOnboardingRequirements_VolunteerOnboardingId",
                table: "VolunteerOnboardingRequirements");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerOnboardingDocuments_VolunteerOnboardingId",
                table: "VolunteerOnboardingDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPolicyAcceptances_VolunteerOnboardingId_VolunteerPolicyId",
                table: "VolunteerPolicyAcceptances",
                columns: new[] { "VolunteerOnboardingId", "VolunteerPolicyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPolicies_Title_Version",
                table: "VolunteerPolicies",
                columns: new[] { "Title", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardings_VolunteerApplicationId",
                table: "VolunteerOnboardings",
                column: "VolunteerApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardingRequirements_VolunteerOnboardingId_DisplayOrder",
                table: "VolunteerOnboardingRequirements",
                columns: new[] { "VolunteerOnboardingId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardingDocuments_VolunteerOnboardingId_DocumentType",
                table: "VolunteerOnboardingDocuments",
                columns: new[] { "VolunteerOnboardingId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOffers_AcceptanceTokenHash",
                table: "VolunteerOffers",
                column: "AcceptanceTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOffers_OfferReference",
                table: "VolunteerOffers",
                column: "OfferReference",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_VolunteerOnboardingId",
                table: "AspNetUsers",
                column: "VolunteerOnboardingId",
                principalTable: "VolunteerOnboardings",
                principalColumn: "VolunteerOnboardingId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOffers_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOffers",
                column: "VolunteerApplicationId",
                principalTable: "VolunteerApplications",
                principalColumn: "VolunteerApplicationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOffers_VolunteerOpportunities_VolunteerOpportunityId",
                table: "VolunteerOffers",
                column: "VolunteerOpportunityId",
                principalTable: "VolunteerOpportunities",
                principalColumn: "VolunteerOpportunityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOnboardings_AspNetUsers_ApplicantId",
                table: "VolunteerOnboardings",
                column: "ApplicantId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOnboardings_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOnboardings",
                column: "VolunteerApplicationId",
                principalTable: "VolunteerApplications",
                principalColumn: "VolunteerApplicationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerPolicyAcceptances_VolunteerPolicies_VolunteerPolicyId",
                table: "VolunteerPolicyAcceptances",
                column: "VolunteerPolicyId",
                principalTable: "VolunteerPolicies",
                principalColumn: "VolunteerPolicyId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_VolunteerOnboardingId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOffers_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOffers_VolunteerOpportunities_VolunteerOpportunityId",
                table: "VolunteerOffers");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOnboardings_AspNetUsers_ApplicantId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerOnboardings_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerPolicyAcceptances_VolunteerPolicies_VolunteerPolicyId",
                table: "VolunteerPolicyAcceptances");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerPolicyAcceptances_VolunteerOnboardingId_VolunteerPolicyId",
                table: "VolunteerPolicyAcceptances");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerPolicies_Title_Version",
                table: "VolunteerPolicies");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerOnboardings_VolunteerApplicationId",
                table: "VolunteerOnboardings");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerOnboardingRequirements_VolunteerOnboardingId_DisplayOrder",
                table: "VolunteerOnboardingRequirements");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerOnboardingDocuments_VolunteerOnboardingId_DocumentType",
                table: "VolunteerOnboardingDocuments");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerOffers_AcceptanceTokenHash",
                table: "VolunteerOffers");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerOffers_OfferReference",
                table: "VolunteerOffers");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPolicyAcceptances_VolunteerOnboardingId",
                table: "VolunteerPolicyAcceptances",
                column: "VolunteerOnboardingId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardings_VolunteerApplicationId",
                table: "VolunteerOnboardings",
                column: "VolunteerApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardingRequirements_VolunteerOnboardingId",
                table: "VolunteerOnboardingRequirements",
                column: "VolunteerOnboardingId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardingDocuments_VolunteerOnboardingId",
                table: "VolunteerOnboardingDocuments",
                column: "VolunteerOnboardingId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_VolunteerOnboardingId",
                table: "AspNetUsers",
                column: "VolunteerOnboardingId",
                principalTable: "VolunteerOnboardings",
                principalColumn: "VolunteerOnboardingId");

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOffers_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOffers",
                column: "VolunteerApplicationId",
                principalTable: "VolunteerApplications",
                principalColumn: "VolunteerApplicationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOffers_VolunteerOpportunities_VolunteerOpportunityId",
                table: "VolunteerOffers",
                column: "VolunteerOpportunityId",
                principalTable: "VolunteerOpportunities",
                principalColumn: "VolunteerOpportunityId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOnboardings_AspNetUsers_ApplicantId",
                table: "VolunteerOnboardings",
                column: "ApplicantId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerOnboardings_VolunteerApplications_VolunteerApplicationId",
                table: "VolunteerOnboardings",
                column: "VolunteerApplicationId",
                principalTable: "VolunteerApplications",
                principalColumn: "VolunteerApplicationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerPolicyAcceptances_VolunteerPolicies_VolunteerPolicyId",
                table: "VolunteerPolicyAcceptances",
                column: "VolunteerPolicyId",
                principalTable: "VolunteerPolicies",
                principalColumn: "VolunteerPolicyId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
