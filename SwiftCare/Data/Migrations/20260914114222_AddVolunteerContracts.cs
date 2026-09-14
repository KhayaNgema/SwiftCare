using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VolunteerContracts",
                columns: table => new
                {
                    VolunteerContractId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerOnboardingId = table.Column<int>(type: "int", nullable: false),
                    VolunteerApplicationId = table.Column<int>(type: "int", nullable: false),
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: false),
                    VolunteerOfferId = table.Column<int>(type: "int", nullable: false),
                    ApplicantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ContractReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContractVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PositionTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RoleDescription = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: true),
                    Area = table.Column<int>(type: "int", nullable: true),
                    CityName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    MinimumAge = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeneratedFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GeneratedFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SignedFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerContracts", x => x.VolunteerContractId);
                    table.ForeignKey(
                        name: "FK_VolunteerContracts_AspNetUsers_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerContracts_VolunteerApplications_VolunteerApplicationId",
                        column: x => x.VolunteerApplicationId,
                        principalTable: "VolunteerApplications",
                        principalColumn: "VolunteerApplicationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerContracts_VolunteerOffers_VolunteerOfferId",
                        column: x => x.VolunteerOfferId,
                        principalTable: "VolunteerOffers",
                        principalColumn: "VolunteerOfferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerContracts_VolunteerOnboardings_VolunteerOnboardingId",
                        column: x => x.VolunteerOnboardingId,
                        principalTable: "VolunteerOnboardings",
                        principalColumn: "VolunteerOnboardingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerContracts_VolunteerOpportunities_VolunteerOpportunityId",
                        column: x => x.VolunteerOpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "VolunteerOpportunityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerContracts_ApplicantId",
                table: "VolunteerContracts",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerContracts_ContractReference",
                table: "VolunteerContracts",
                column: "ContractReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerContracts_VolunteerApplicationId",
                table: "VolunteerContracts",
                column: "VolunteerApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerContracts_VolunteerOfferId",
                table: "VolunteerContracts",
                column: "VolunteerOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerContracts_VolunteerOnboardingId",
                table: "VolunteerContracts",
                column: "VolunteerOnboardingId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerContracts_VolunteerOpportunityId",
                table: "VolunteerContracts",
                column: "VolunteerOpportunityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerContracts");
        }
    }
}
