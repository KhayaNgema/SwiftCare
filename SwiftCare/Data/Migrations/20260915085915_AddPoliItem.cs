using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPoliItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "VolunteerPolicies");

            migrationBuilder.CreateTable(
                name: "VolunteerPolicyItems",
                columns: table => new
                {
                    VolunteerPolicyItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerPolicyId = table.Column<int>(type: "int", nullable: false),
                    Heading = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    IsSubheading = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerPolicyItems", x => x.VolunteerPolicyItemId);
                    table.ForeignKey(
                        name: "FK_VolunteerPolicyItems_VolunteerPolicies_VolunteerPolicyId",
                        column: x => x.VolunteerPolicyId,
                        principalTable: "VolunteerPolicies",
                        principalColumn: "VolunteerPolicyId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPolicyItems_VolunteerPolicyId",
                table: "VolunteerPolicyItems",
                column: "VolunteerPolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerPolicyItems");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "VolunteerPolicies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
