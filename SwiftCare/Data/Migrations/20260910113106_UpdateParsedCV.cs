using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateParsedCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParsedCv_AspNetUsers_ApplicantId1",
                table: "ParsedCv");

            migrationBuilder.DropIndex(
                name: "IX_ParsedCv_ApplicantId1",
                table: "ParsedCv");

            migrationBuilder.DropColumn(
                name: "ApplicantId1",
                table: "ParsedCv");

            migrationBuilder.AlterColumn<string>(
                name: "ApplicantId",
                table: "ParsedCv",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_ParsedCv_ApplicantId",
                table: "ParsedCv",
                column: "ApplicantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParsedCv_AspNetUsers_ApplicantId",
                table: "ParsedCv",
                column: "ApplicantId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParsedCv_AspNetUsers_ApplicantId",
                table: "ParsedCv");

            migrationBuilder.DropIndex(
                name: "IX_ParsedCv_ApplicantId",
                table: "ParsedCv");

            migrationBuilder.AlterColumn<int>(
                name: "ApplicantId",
                table: "ParsedCv",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ApplicantId1",
                table: "ParsedCv",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParsedCv_ApplicantId1",
                table: "ParsedCv",
                column: "ApplicantId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ParsedCv_AspNetUsers_ApplicantId1",
                table: "ParsedCv",
                column: "ApplicantId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
