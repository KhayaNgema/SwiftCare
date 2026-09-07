using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddATS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AIRecommendation",
                table: "VolunteerApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ATSRanking",
                table: "VolunteerApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ATSScore",
                table: "VolunteerApplications",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdminDecision",
                table: "VolunteerApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DecisionDate",
                table: "VolunteerApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EligibilityStatus",
                table: "VolunteerApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewDate",
                table: "VolunteerApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEvaluatedDate",
                table: "VolunteerApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScreeningStatus",
                table: "VolunteerApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShortlistedDate",
                table: "VolunteerApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Stage",
                table: "VolunteerApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VolunteerApplicationAudits",
                columns: table => new
                {
                    VolunteerApplicationAuditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerApplicationId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerApplicationAudits", x => x.VolunteerApplicationAuditId);
                    table.ForeignKey(
                        name: "FK_VolunteerApplicationAudits_VolunteerApplications_VolunteerApplicationId",
                        column: x => x.VolunteerApplicationId,
                        principalTable: "VolunteerApplications",
                        principalColumn: "VolunteerApplicationId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerApplicationEvaluations",
                columns: table => new
                {
                    VolunteerApplicationEvaluationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerApplicationId = table.Column<int>(type: "int", nullable: false),
                    EligibilityStatus = table.Column<int>(type: "int", nullable: false),
                    EligibilityScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QualificationScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExperienceScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SkillsScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuestionScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailabilityScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MotivationScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RankingPosition = table.Column<int>(type: "int", nullable: true),
                    AIRecommendation = table.Column<int>(type: "int", nullable: false),
                    AIConfidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AIStrengths = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    AIConcerns = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    AIExplanation = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    AIRequirementMatches = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    EvaluatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EvaluationVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerApplicationEvaluations", x => x.VolunteerApplicationEvaluationId);
                    table.ForeignKey(
                        name: "FK_VolunteerApplicationEvaluations_VolunteerApplications_VolunteerApplicationId",
                        column: x => x.VolunteerApplicationId,
                        principalTable: "VolunteerApplications",
                        principalColumn: "VolunteerApplicationId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerOpportunityScoringConfigurations",
                columns: table => new
                {
                    VolunteerOpportunityScoringConfigurationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: false),
                    QualificationWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExperienceWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SkillsWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuestionWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailabilityWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MotivationWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerOpportunityScoringConfigurations", x => x.VolunteerOpportunityScoringConfigurationId);
                    table.ForeignKey(
                        name: "FK_VolunteerOpportunityScoringConfigurations_VolunteerOpportunities_VolunteerOpportunityId",
                        column: x => x.VolunteerOpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "VolunteerOpportunityId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerApplicationAudits_VolunteerApplicationId",
                table: "VolunteerApplicationAudits",
                column: "VolunteerApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerApplicationEvaluations_VolunteerApplicationId",
                table: "VolunteerApplicationEvaluations",
                column: "VolunteerApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOpportunityScoringConfigurations_VolunteerOpportunityId",
                table: "VolunteerOpportunityScoringConfigurations",
                column: "VolunteerOpportunityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerApplicationAudits");

            migrationBuilder.DropTable(
                name: "VolunteerApplicationEvaluations");

            migrationBuilder.DropTable(
                name: "VolunteerOpportunityScoringConfigurations");

            migrationBuilder.DropColumn(
                name: "AIRecommendation",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "ATSRanking",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "ATSScore",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "AdminDecision",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "DecisionDate",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "EligibilityStatus",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "InterviewDate",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "LastEvaluatedDate",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "ScreeningStatus",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "ShortlistedDate",
                table: "VolunteerApplications");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "VolunteerApplications");
        }
    }
}
