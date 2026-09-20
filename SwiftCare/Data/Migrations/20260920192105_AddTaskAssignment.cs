using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskAssignment : Migration
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

            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerTasks_VolunteerRosters_VolunteerRosterId",
                table: "VolunteerTasks");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerTasks_AssignedById",
                table: "VolunteerTasks");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerTasks_VolunteerId",
                table: "VolunteerTasks");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerTasks_VolunteerRosterId",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "AssignedById",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "StartedDate",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "TaskDate",
                table: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "VolunteerId",
                table: "VolunteerTasks");

            migrationBuilder.RenameColumn(
                name: "VolunteerRosterId",
                table: "VolunteerTasks",
                newName: "Area");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "VolunteerTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "VolunteerTaskAssignments",
                columns: table => new
                {
                    VolunteerTaskAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerTaskId = table.Column<int>(type: "int", nullable: false),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    VolunteerRosterId = table.Column<int>(type: "int", nullable: true),
                    TaskDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssignedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VolunteerId1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerTaskAssignments", x => x.VolunteerTaskAssignmentId);
                    table.ForeignKey(
                        name: "FK_VolunteerTaskAssignments_AspNetUsers_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VolunteerTaskAssignments_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerTaskAssignments_AspNetUsers_VolunteerId1",
                        column: x => x.VolunteerId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VolunteerTaskAssignments_VolunteerRosters_VolunteerRosterId",
                        column: x => x.VolunteerRosterId,
                        principalTable: "VolunteerRosters",
                        principalColumn: "VolunteerRosterId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VolunteerTaskAssignments_VolunteerTasks_VolunteerTaskId",
                        column: x => x.VolunteerTaskId,
                        principalTable: "VolunteerTasks",
                        principalColumn: "VolunteerTaskId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTaskAssignments_AssignedById",
                table: "VolunteerTaskAssignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTaskAssignments_VolunteerId",
                table: "VolunteerTaskAssignments",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTaskAssignments_VolunteerId1",
                table: "VolunteerTaskAssignments",
                column: "VolunteerId1");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTaskAssignments_VolunteerRosterId",
                table: "VolunteerTaskAssignments",
                column: "VolunteerRosterId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTaskAssignments_VolunteerTaskId",
                table: "VolunteerTaskAssignments",
                column: "VolunteerTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerTaskAssignments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "VolunteerTasks");

            migrationBuilder.RenameColumn(
                name: "Area",
                table: "VolunteerTasks",
                newName: "VolunteerRosterId");

            migrationBuilder.AddColumn<string>(
                name: "AssignedById",
                table: "VolunteerTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "VolunteerTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "VolunteerTasks",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "VolunteerTasks",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "VolunteerTasks",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedDate",
                table: "VolunteerTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "VolunteerTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TaskDate",
                table: "VolunteerTasks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "VolunteerId",
                table: "VolunteerTasks",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTasks_AssignedById",
                table: "VolunteerTasks",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTasks_VolunteerId",
                table: "VolunteerTasks",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTasks_VolunteerRosterId",
                table: "VolunteerTasks",
                column: "VolunteerRosterId");

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
    }
}
