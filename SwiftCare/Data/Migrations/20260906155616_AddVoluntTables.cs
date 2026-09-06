using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoluntTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactRelationship",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Motivation",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OnboardingVolunteerOnboardingId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredArea",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    CityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    GeoNameId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.CityId);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerAgreements",
                columns: table => new
                {
                    VolunteerAgreementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgreementName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Accepted = table.Column<bool>(type: "bit", nullable: false),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgreementFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerAgreements", x => x.VolunteerAgreementId);
                    table.ForeignKey(
                        name: "FK_VolunteerAgreements_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerAvailabilities",
                columns: table => new
                {
                    VolunteerAvailabilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerAvailabilities", x => x.VolunteerAvailabilityId);
                    table.ForeignKey(
                        name: "FK_VolunteerAvailabilities_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerDocuments",
                columns: table => new
                {
                    VolunteerDocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicantId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerDocuments", x => x.VolunteerDocumentId);
                    table.ForeignKey(
                        name: "FK_VolunteerDocuments_AspNetUsers_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerExperiences",
                columns: table => new
                {
                    VolunteerExperienceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Position = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Organization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerExperiences", x => x.VolunteerExperienceId);
                    table.ForeignKey(
                        name: "FK_VolunteerExperiences_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerOnboardings",
                columns: table => new
                {
                    VolunteerOnboardingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProfileCompleted = table.Column<bool>(type: "bit", nullable: false),
                    DocumentsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    ScreeningCompleted = table.Column<bool>(type: "bit", nullable: false),
                    AgreementCompleted = table.Column<bool>(type: "bit", nullable: false),
                    OrientationCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApplicantId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerOnboardings", x => x.VolunteerOnboardingId);
                    table.ForeignKey(
                        name: "FK_VolunteerOnboardings_AspNetUsers_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerOrientations",
                columns: table => new
                {
                    VolunteerOrientationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrientationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConductedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerOrientations", x => x.VolunteerOrientationId);
                    table.ForeignKey(
                        name: "FK_VolunteerOrientations_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerScreenings",
                columns: table => new
                {
                    VolunteerScreeningId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ScreeningDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompletedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerScreenings", x => x.VolunteerScreeningId);
                    table.ForeignKey(
                        name: "FK_VolunteerScreenings_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerSkills",
                columns: table => new
                {
                    VolunteerSkillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Skill = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerSkills", x => x.VolunteerSkillId);
                    table.ForeignKey(
                        name: "FK_VolunteerSkills_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerOpportunities",
                columns: table => new
                {
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: true),
                    Area = table.Column<int>(type: "int", nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    VolunteersNeeded = table.Column<int>(type: "int", nullable: true),
                    VolunteersAccepted = table.Column<int>(type: "int", nullable: false),
                    MinimumAge = table.Column<int>(type: "int", nullable: true),
                    ApplicationDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PosterImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerOpportunities", x => x.VolunteerOpportunityId);
                    table.ForeignKey(
                        name: "FK_VolunteerOpportunities_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "CityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerApplications",
                columns: table => new
                {
                    VolunteerApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppliedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CVFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CVFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CVUploadedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApplicantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: false),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerApplications", x => x.VolunteerApplicationId);
                    table.ForeignKey(
                        name: "FK_VolunteerApplications_AspNetUsers_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_VolunteerApplications_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VolunteerApplications_VolunteerOpportunities_VolunteerOpportunityId",
                        column: x => x.VolunteerOpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "VolunteerOpportunityId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerRequiredSkills",
                columns: table => new
                {
                    VolunteerRequiredSkillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Skill = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerRequiredSkills", x => x.VolunteerRequiredSkillId);
                    table.ForeignKey(
                        name: "FK_VolunteerRequiredSkills_VolunteerOpportunities_VolunteerOpportunityId",
                        column: x => x.VolunteerOpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "VolunteerOpportunityId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerRequirements",
                columns: table => new
                {
                    VolunteerRequirementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerRequirements", x => x.VolunteerRequirementId);
                    table.ForeignKey(
                        name: "FK_VolunteerRequirements_VolunteerOpportunities_VolunteerOpportunityId",
                        column: x => x.VolunteerOpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "VolunteerOpportunityId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerResponsibilities",
                columns: table => new
                {
                    VolunteerResponsibilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerResponsibilities", x => x.VolunteerResponsibilityId);
                    table.ForeignKey(
                        name: "FK_VolunteerResponsibilities_VolunteerOpportunities_VolunteerOpportunityId",
                        column: x => x.VolunteerOpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "VolunteerOpportunityId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerTasks",
                columns: table => new
                {
                    VolunteerTaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerTasks", x => x.VolunteerTaskId);
                    table.ForeignKey(
                        name: "FK_VolunteerTasks_AspNetUsers_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VolunteerTasks_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_VolunteerTasks_VolunteerOpportunities_VolunteerOpportunityId",
                        column: x => x.VolunteerOpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "VolunteerOpportunityId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OnboardingVolunteerOnboardingId",
                table: "AspNetUsers",
                column: "OnboardingVolunteerOnboardingId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAgreements_VolunteerId",
                table: "VolunteerAgreements",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerApplications_ApplicantId_VolunteerOpportunityId",
                table: "VolunteerApplications",
                columns: new[] { "ApplicantId", "VolunteerOpportunityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerApplications_VolunteerId",
                table: "VolunteerApplications",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerApplications_VolunteerOpportunityId",
                table: "VolunteerApplications",
                column: "VolunteerOpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAvailabilities_VolunteerId",
                table: "VolunteerAvailabilities",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerDocuments_ApplicantId",
                table: "VolunteerDocuments",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerExperiences_VolunteerId",
                table: "VolunteerExperiences",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOnboardings_ApplicantId",
                table: "VolunteerOnboardings",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOpportunities_CityId",
                table: "VolunteerOpportunities",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOrientations_VolunteerId",
                table: "VolunteerOrientations",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerRequiredSkills_VolunteerOpportunityId",
                table: "VolunteerRequiredSkills",
                column: "VolunteerOpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerRequirements_VolunteerOpportunityId",
                table: "VolunteerRequirements",
                column: "VolunteerOpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerResponsibilities_VolunteerOpportunityId",
                table: "VolunteerResponsibilities",
                column: "VolunteerOpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerScreenings_VolunteerId",
                table: "VolunteerScreenings",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerSkills_VolunteerId",
                table: "VolunteerSkills",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTasks_AssignedById",
                table: "VolunteerTasks",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTasks_VolunteerId",
                table: "VolunteerTasks",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTasks_VolunteerOpportunityId",
                table: "VolunteerTasks",
                column: "VolunteerOpportunityId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_OnboardingVolunteerOnboardingId",
                table: "AspNetUsers",
                column: "OnboardingVolunteerOnboardingId",
                principalTable: "VolunteerOnboardings",
                principalColumn: "VolunteerOnboardingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_VolunteerOnboardings_OnboardingVolunteerOnboardingId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "VolunteerAgreements");

            migrationBuilder.DropTable(
                name: "VolunteerApplications");

            migrationBuilder.DropTable(
                name: "VolunteerAvailabilities");

            migrationBuilder.DropTable(
                name: "VolunteerDocuments");

            migrationBuilder.DropTable(
                name: "VolunteerExperiences");

            migrationBuilder.DropTable(
                name: "VolunteerOnboardings");

            migrationBuilder.DropTable(
                name: "VolunteerOrientations");

            migrationBuilder.DropTable(
                name: "VolunteerRequiredSkills");

            migrationBuilder.DropTable(
                name: "VolunteerRequirements");

            migrationBuilder.DropTable(
                name: "VolunteerResponsibilities");

            migrationBuilder.DropTable(
                name: "VolunteerScreenings");

            migrationBuilder.DropTable(
                name: "VolunteerSkills");

            migrationBuilder.DropTable(
                name: "VolunteerTasks");

            migrationBuilder.DropTable(
                name: "VolunteerOpportunities");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OnboardingVolunteerOnboardingId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmergencyContactRelationship",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Motivation",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OnboardingVolunteerOnboardingId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredArea",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AspNetUsers");
        }
    }
}
