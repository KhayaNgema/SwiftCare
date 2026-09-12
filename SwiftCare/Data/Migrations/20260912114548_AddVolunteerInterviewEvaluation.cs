using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerInterviewEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VolunteerInterviewEvaluations",
                columns: table => new
                {
                    VolunteerInterviewEvaluationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerApplicationId = table.Column<int>(type: "int", nullable: false),
                    VolunteerVoiceInterviewSessionId = table.Column<int>(type: "int", nullable: true),
                    OverallInterviewScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverallInterviewConfidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EvaluationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    EvaluationVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EvaluationProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EvaluationModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FoundryEvaluationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EvaluationDatasetVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EvaluationPromptVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EvaluationPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EvaluatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerInterviewEvaluations", x => x.VolunteerInterviewEvaluationId);
                    table.ForeignKey(
                        name: "FK_VolunteerInterviewEvaluations_VolunteerApplications_VolunteerApplicationId",
                        column: x => x.VolunteerApplicationId,
                        principalTable: "VolunteerApplications",
                        principalColumn: "VolunteerApplicationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerInterviewEvaluations_VolunteerVoiceInterviewSessions_VolunteerVoiceInterviewSessionId",
                        column: x => x.VolunteerVoiceInterviewSessionId,
                        principalTable: "VolunteerVoiceInterviewSessions",
                        principalColumn: "VolunteerVoiceInterviewSessionId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerInterviewCompetencyEvaluations",
                columns: table => new
                {
                    VolunteerInterviewCompetencyEvaluationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerInterviewEvaluationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Assessment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Reasoning = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Demonstrated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerInterviewCompetencyEvaluations", x => x.VolunteerInterviewCompetencyEvaluationId);
                    table.ForeignKey(
                        name: "FK_VolunteerInterviewCompetencyEvaluations_VolunteerInterviewEvaluations_VolunteerInterviewEvaluationId",
                        column: x => x.VolunteerInterviewEvaluationId,
                        principalTable: "VolunteerInterviewEvaluations",
                        principalColumn: "VolunteerInterviewEvaluationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerInterviewSkillEvaluations",
                columns: table => new
                {
                    VolunteerInterviewSkillEvaluationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerInterviewEvaluationId = table.Column<int>(type: "int", nullable: false),
                    VolunteerRequiredSkillId = table.Column<int>(type: "int", nullable: false),
                    Skill = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FoundInCv = table.Column<bool>(type: "bit", nullable: false),
                    CvEvidence = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    FoundInInterview = table.Column<bool>(type: "bit", nullable: false),
                    InterviewEvidence = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    InterviewScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Assessment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Reasoning = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerInterviewSkillEvaluations", x => x.VolunteerInterviewSkillEvaluationId);
                    table.ForeignKey(
                        name: "FK_VolunteerInterviewSkillEvaluations_VolunteerInterviewEvaluations_VolunteerInterviewEvaluationId",
                        column: x => x.VolunteerInterviewEvaluationId,
                        principalTable: "VolunteerInterviewEvaluations",
                        principalColumn: "VolunteerInterviewEvaluationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VolunteerInterviewSkillEvaluations_VolunteerRequiredSkills_VolunteerRequiredSkillId",
                        column: x => x.VolunteerRequiredSkillId,
                        principalTable: "VolunteerRequiredSkills",
                        principalColumn: "VolunteerRequiredSkillId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerInterviewCompetencyEvaluations_VolunteerInterviewEvaluationId",
                table: "VolunteerInterviewCompetencyEvaluations",
                column: "VolunteerInterviewEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerInterviewEvaluations_VolunteerApplicationId",
                table: "VolunteerInterviewEvaluations",
                column: "VolunteerApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerInterviewEvaluations_VolunteerVoiceInterviewSessionId",
                table: "VolunteerInterviewEvaluations",
                column: "VolunteerVoiceInterviewSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerInterviewSkillEvaluations_VolunteerInterviewEvaluationId",
                table: "VolunteerInterviewSkillEvaluations",
                column: "VolunteerInterviewEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerInterviewSkillEvaluations_VolunteerRequiredSkillId",
                table: "VolunteerInterviewSkillEvaluations",
                column: "VolunteerRequiredSkillId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerInterviewCompetencyEvaluations");

            migrationBuilder.DropTable(
                name: "VolunteerInterviewSkillEvaluations");

            migrationBuilder.DropTable(
                name: "VolunteerInterviewEvaluations");
        }
    }
}
