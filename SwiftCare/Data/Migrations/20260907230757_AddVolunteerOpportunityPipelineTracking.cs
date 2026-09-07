using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerOpportunityPipelineTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShortlistingPipelineRun",
                table: "VolunteerOpportunities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShortlistingPipelineRunBy",
                table: "VolunteerOpportunities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShortlistingPipelineRunDate",
                table: "VolunteerOpportunities",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortlistingPipelineRun",
                table: "VolunteerOpportunities");

            migrationBuilder.DropColumn(
                name: "ShortlistingPipelineRunBy",
                table: "VolunteerOpportunities");

            migrationBuilder.DropColumn(
                name: "ShortlistingPipelineRunDate",
                table: "VolunteerOpportunities");
        }
    }
}
