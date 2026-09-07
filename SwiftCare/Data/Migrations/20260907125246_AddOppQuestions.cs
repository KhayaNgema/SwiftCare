using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOppQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverMessage",
                table: "VolunteerApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "VolunteerQuestions",
                columns: table => new
                {
                    VolunteerQuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: true),
                    Area = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerQuestions", x => x.VolunteerQuestionId);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerApplicationAnswers",
                columns: table => new
                {
                    VolunteerApplicationAnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerApplicationId = table.Column<int>(type: "int", nullable: false),
                    VolunteerQuestionId = table.Column<int>(type: "int", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerApplicationAnswers", x => x.VolunteerApplicationAnswerId);
                    table.ForeignKey(
                        name: "FK_VolunteerApplicationAnswers_VolunteerApplications_VolunteerApplicationId",
                        column: x => x.VolunteerApplicationId,
                        principalTable: "VolunteerApplications",
                        principalColumn: "VolunteerApplicationId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_VolunteerApplicationAnswers_VolunteerQuestions_VolunteerQuestionId",
                        column: x => x.VolunteerQuestionId,
                        principalTable: "VolunteerQuestions",
                        principalColumn: "VolunteerQuestionId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerOpportunityQuestions",
                columns: table => new
                {
                    VolunteerOpportunityQuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerOpportunityId = table.Column<int>(type: "int", nullable: false),
                    VolunteerQuestionId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerOpportunityQuestions", x => x.VolunteerOpportunityQuestionId);
                    table.ForeignKey(
                        name: "FK_VolunteerOpportunityQuestions_VolunteerOpportunities_VolunteerOpportunityId",
                        column: x => x.VolunteerOpportunityId,
                        principalTable: "VolunteerOpportunities",
                        principalColumn: "VolunteerOpportunityId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_VolunteerOpportunityQuestions_VolunteerQuestions_VolunteerQuestionId",
                        column: x => x.VolunteerQuestionId,
                        principalTable: "VolunteerQuestions",
                        principalColumn: "VolunteerQuestionId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerApplicationAnswers_VolunteerApplicationId",
                table: "VolunteerApplicationAnswers",
                column: "VolunteerApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerApplicationAnswers_VolunteerQuestionId",
                table: "VolunteerApplicationAnswers",
                column: "VolunteerQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOpportunityQuestions_VolunteerOpportunityId",
                table: "VolunteerOpportunityQuestions",
                column: "VolunteerOpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerOpportunityQuestions_VolunteerQuestionId",
                table: "VolunteerOpportunityQuestions",
                column: "VolunteerQuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerApplicationAnswers");

            migrationBuilder.DropTable(
                name: "VolunteerOpportunityQuestions");

            migrationBuilder.DropTable(
                name: "VolunteerQuestions");

            migrationBuilder.DropColumn(
                name: "CoverMessage",
                table: "VolunteerApplications");
        }
    }
}
