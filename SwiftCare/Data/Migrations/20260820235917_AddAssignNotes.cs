using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentNotes",
                table: "OurEquipment",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentNotes",
                table: "OurEquipment");
        }
    }
}
