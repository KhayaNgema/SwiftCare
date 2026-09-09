using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCVPars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Languages",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "ParsedCv");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CareerObjective",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubUrl",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortfolioUrl",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalSummary",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalTitle",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CvLanguage",
                columns: table => new
                {
                    CvLanguageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParsedCvId = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvLanguage", x => x.CvLanguageId);
                    table.ForeignKey(
                        name: "FK_CvLanguage_ParsedCv_ParsedCvId",
                        column: x => x.ParsedCvId,
                        principalTable: "ParsedCv",
                        principalColumn: "ParsedCvId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "CvSkill",
                columns: table => new
                {
                    CvSkillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParsedCvId = table.Column<int>(type: "int", nullable: false),
                    Skill = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvSkill", x => x.CvSkillId);
                    table.ForeignKey(
                        name: "FK_CvSkill_ParsedCv_ParsedCvId",
                        column: x => x.ParsedCvId,
                        principalTable: "ParsedCv",
                        principalColumn: "ParsedCvId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CvLanguage_ParsedCvId",
                table: "CvLanguage",
                column: "ParsedCvId");

            migrationBuilder.CreateIndex(
                name: "IX_CvSkill_ParsedCvId",
                table: "CvSkill",
                column: "ParsedCvId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CvLanguage");

            migrationBuilder.DropTable(
                name: "CvSkill");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "CareerObjective",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "City",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "GitHubUrl",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "LinkedInUrl",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "PortfolioUrl",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "ProfessionalSummary",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "ProfessionalTitle",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "ParsedCv");

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "ParsedCv",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
