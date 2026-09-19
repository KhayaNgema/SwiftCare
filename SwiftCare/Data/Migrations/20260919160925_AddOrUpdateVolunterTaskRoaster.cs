using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrUpdateVolunterTaskRoaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerTasks_AspNetUsers_AssignedById",
                table: "VolunteerTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerTasks_AspNetUsers_VolunteerId",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "VolunteerTasks");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "VolunteerTasks",
                newName: "StartedDate");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "VolunteerTasks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "VolunteerTasks",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "VolunteerTasks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "VolunteerTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "VolunteerTasks",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "TaskDate",
                table: "VolunteerTasks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "VolunteerRosterId",
                table: "VolunteerTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Area",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "AspNetUsers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Volunteer_Bio",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Volunteer_EndDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VolunteerRosters",
                columns: table => new
                {
                    VolunteerRosterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RosterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SupervisorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerRosters", x => x.VolunteerRosterId);
                    table.ForeignKey(
                        name: "FK_VolunteerRosters_AspNetUsers_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerRosters_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTasks_VolunteerRosterId",
                table: "VolunteerTasks",
                column: "VolunteerRosterId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SupervisorId",
                table: "AspNetUsers",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerRosters_SupervisorId",
                table: "VolunteerRosters",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerRosters_VolunteerId",
                table: "VolunteerRosters",
                column: "VolunteerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_SupervisorId",
                table: "AspNetUsers",
                column: "SupervisorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerTasks_AspNetUsers_AssignedById",
                table: "VolunteerTasks",
                column: "AssignedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerTasks_AspNetUsers_VolunteerId",
                table: "VolunteerTasks",
                column: "VolunteerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerTasks_VolunteerRosters_VolunteerRosterId",
                table: "VolunteerTasks",
                column: "VolunteerRosterId",
                principalTable: "VolunteerRosters",
                principalColumn: "VolunteerRosterId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_SupervisorId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerTasks_AspNetUsers_AssignedById",
                table: "VolunteerTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerTasks_AspNetUsers_VolunteerId",
                table: "VolunteerTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerTasks_VolunteerRosters_VolunteerRosterId",
                table: "VolunteerTasks");

            migrationBuilder.DropTable(
                name: "VolunteerRosters");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerTasks_VolunteerRosterId",
                table: "VolunteerTasks");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SupervisorId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "TaskDate",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "VolunteerRosterId",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Volunteer_Bio",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Volunteer_EndDate",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "StartedDate",
                table: "VolunteerTasks",
                newName: "DueDate");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "VolunteerTasks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "VolunteerTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerTasks_AspNetUsers_AssignedById",
                table: "VolunteerTasks",
                column: "AssignedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerTasks_AspNetUsers_VolunteerId",
                table: "VolunteerTasks",
                column: "VolunteerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
