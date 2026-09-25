using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewDetectionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FullScreenExitCount",
                table: "VolunteerVoiceInterviewSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PageLeaveCount",
                table: "VolunteerVoiceInterviewSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
                name: "ProctoringFlagReason",
                table: "VolunteerVoiceInterviewSessions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProctoringFlaggedDate",
                table: "VolunteerVoiceInterviewSessions",
                type: "datetime2",
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

            migrationBuilder.AddColumn<int>(
                name: "SuspiciousEventCount",
                table: "VolunteerVoiceInterviewSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TabSwitchCount",
                table: "VolunteerVoiceInterviewSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "VideoRecordingCompleted",
                table: "VolunteerVoiceInterviewSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VideoRecordingCompletedDate",
                table: "VolunteerVoiceInterviewSessions",
                type: "datetime2",
                nullable: true);

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

            migrationBuilder.AddColumn<bool>(
                name: "VideoRecordingStarted",
                table: "VolunteerVoiceInterviewSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VideoRecordingStartedDate",
                table: "VolunteerVoiceInterviewSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WindowBlurCount",
                table: "VolunteerVoiceInterviewSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WindowFocusCount",
                table: "VolunteerVoiceInterviewSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullScreenExitCount",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "PageLeaveCount",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "ProctoringEnabled",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "ProctoringEventsJson",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "ProctoringFlagReason",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "ProctoringFlaggedDate",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "ProctoringStatus",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "RequiresProctoringReview",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "SuspiciousEventCount",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "TabSwitchCount",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "VideoRecordingCompleted",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "VideoRecordingCompletedDate",
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

            migrationBuilder.DropColumn(
                name: "VideoRecordingStarted",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "VideoRecordingStartedDate",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "WindowBlurCount",
                table: "VolunteerVoiceInterviewSessions");

            migrationBuilder.DropColumn(
                name: "WindowFocusCount",
                table: "VolunteerVoiceInterviewSessions");
        }
    }
}
