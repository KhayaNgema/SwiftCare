using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerInterviewProctoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProctoringEnabled",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "ProctoringEventsJson",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "ProctoringStatus",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "RequiresProctoringReview",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "VideoRecordingCompleted",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "VideoRecordingDurationSeconds",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "VideoRecordingEnabled",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "VideoRecordingPath",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.RenameColumn(
                name: "WindowFocusCount",
                table: "VolunteerVoiceInterviewSessions",
                newName: "VideoDurationSeconds");

            migrationBuilder.RenameColumn(
                name: "VideoRecordingStartedDate",
                table: "VolunteerVoiceInterviewSessions",
                newName: "VideoStartedDate");

            migrationBuilder.RenameColumn(
                name: "VideoRecordingStarted",
                table: "VolunteerVoiceInterviewSessions",
                newName: "SuspiciousActivityDetected");

            migrationBuilder.RenameColumn(
                name: "VideoRecordingCompletedDate",
                table: "VolunteerVoiceInterviewSessions",
                newName: "VideoCompletedDate");

            migrationBuilder.RenameColumn(
                name: "ProctoringFlaggedDate",
                table: "VolunteerVoiceInterviewSessions",
                newName: "SuspiciousActivityDate");

            migrationBuilder.RenameColumn(
                name: "ProctoringFlagReason",
                table: "VolunteerVoiceInterviewSessions",
                newName: "VideoFilePath");

            migrationBuilder.AddColumn<int>(
                name: "CameraStoppedCount",
                table: "VolunteerVoiceInterviewSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IncompleteReason",
                table: "VolunteerVoiceInterviewSessions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityDate",
                table: "VolunteerVoiceInterviewSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastActivityType",
                table: "VolunteerVoiceInterviewSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastProctoringEventType",
                table: "VolunteerVoiceInterviewSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalUnfocusedSeconds",
                table: "VolunteerVoiceInterviewSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VideoMimeType",
                table: "VolunteerVoiceInterviewSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VolunteerInterviewProctoringEvents",
                columns: table => new
                {
                    VolunteerInterviewProctoringEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VolunteerVoiceInterviewSessionId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PageVisibilityState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PageHidden = table.Column<bool>(type: "bit", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnfocusedSeconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerInterviewProctoringEvents", x => x.VolunteerInterviewProctoringEventId);
                    table.ForeignKey(
                        name: "FK_VolunteerInterviewProctoringEvents_VolunteerVoiceInterviewSessions_VolunteerVoiceInterviewSessionId",
                        column: x => x.VolunteerVoiceInterviewSessionId,
                        principalTable: "VolunteerVoiceInterviewSessions",
                        principalColumn: "VolunteerVoiceInterviewSessionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerInterviewProctoringEvents_VolunteerVoiceInterviewSessionId",
                table: "VolunteerInterviewProctoringEvents",
                column: "VolunteerVoiceInterviewSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerInterviewProctoringEvents");

            migrationBuilder.DropColumn(
                name: "CameraStoppedCount",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "IncompleteReason",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "LastActivityDate",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "LastActivityType",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "LastProctoringEventType",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "TotalUnfocusedSeconds",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "VideoMimeType",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.RenameColumn(
                name: "VideoStartedDate",
                table: "VolunteerVoiceInterviewSessions",
                newName: "VideoRecordingStartedDate");

            migrationBuilder.RenameColumn(
                name: "VideoFilePath",
                table: "VolunteerVoiceInterviewSessions",
                newName: "ProctoringFlagReason");

            migrationBuilder.RenameColumn(
                name: "VideoDurationSeconds",
                table: "VolunteerVoiceInterviewSessions",
                newName: "WindowFocusCount");

            migrationBuilder.RenameColumn(
                name: "VideoCompletedDate",
                table: "VolunteerVoiceInterviewSessions",
                newName: "VideoRecordingCompletedDate");

            migrationBuilder.RenameColumn(
                name: "SuspiciousActivityDetected",
                table: "VolunteerVoiceInterviewSessions",
                newName: "VideoRecordingStarted");

            migrationBuilder.RenameColumn(
                name: "SuspiciousActivityDate",
                table: "VolunteerVoiceInterviewSessions",
                newName: "ProctoringFlaggedDate");

            migrationBuilder.AddColumn<bool>(
                name: "ProctoringEnabled",
                table: "VolunteerVoiceInterviewSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProctoringEventsJson",
                table: "VolunteerVoiceInterviewSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProctoringStatus",
                table: "VolunteerVoiceInterviewSessions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresProctoringReview",
                table: "VolunteerVoiceInterviewSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VideoRecordingCompleted",
                table: "VolunteerVoiceInterviewSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VideoRecordingDurationSeconds",
                table: "VolunteerVoiceInterviewSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VideoRecordingEnabled",
                table: "VolunteerVoiceInterviewSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VideoRecordingPath",
                table: "VolunteerVoiceInterviewSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
