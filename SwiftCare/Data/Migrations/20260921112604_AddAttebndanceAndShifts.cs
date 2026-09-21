using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttebndanceAndShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupervisorId1",
                table: "VolunteerRosters",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VolunteerRosterShifts",
                columns: table => new
                {
                    VolunteerRosterShiftId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerRosterId = table.Column<int>(type: "int", nullable: false),
                    ShiftDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerRosterShifts", x => x.VolunteerRosterShiftId);
                    table.ForeignKey(
                        name: "FK_VolunteerRosterShifts_VolunteerRosters_VolunteerRosterId",
                        column: x => x.VolunteerRosterId,
                        principalTable: "VolunteerRosters",
                        principalColumn: "VolunteerRosterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerAttendances",
                columns: table => new
                {
                    AttendanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    VolunteerRosterShiftId = table.Column<int>(type: "int", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    EarlyDepartureMinutes = table.Column<int>(type: "int", nullable: false),
                    FaceVerified = table.Column<bool>(type: "bit", nullable: false),
                    LivenessVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerificationProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VerificationReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerAttendances", x => x.AttendanceId);
                    table.ForeignKey(
                        name: "FK_VolunteerAttendances_AspNetUsers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VolunteerAttendances_VolunteerRosterShifts_VolunteerRosterShiftId",
                        column: x => x.VolunteerRosterShiftId,
                        principalTable: "VolunteerRosterShifts",
                        principalColumn: "VolunteerRosterShiftId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerRosters_SupervisorId1",
                table: "VolunteerRosters",
                column: "SupervisorId1");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAttendances_VolunteerId",
                table: "VolunteerAttendances",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAttendances_VolunteerRosterShiftId",
                table: "VolunteerAttendances",
                column: "VolunteerRosterShiftId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerRosterShifts_VolunteerRosterId",
                table: "VolunteerRosterShifts",
                column: "VolunteerRosterId");

            migrationBuilder.AddForeignKey(
                name: "FK_VolunteerRosters_AspNetUsers_SupervisorId1",
                table: "VolunteerRosters",
                column: "SupervisorId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VolunteerRosters_AspNetUsers_SupervisorId1",
                table: "VolunteerRosters");

            migrationBuilder.DropTable(
                name: "VolunteerAttendances");

            migrationBuilder.DropTable(
                name: "VolunteerRosterShifts");

            migrationBuilder.DropIndex(
                name: "IX_VolunteerRosters_SupervisorId1",
                table: "VolunteerRosters");

            migrationBuilder.DropColumn(
                name: "SupervisorId1",
                table: "VolunteerRosters");
        }
    }
}
