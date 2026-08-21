using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignEquip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "OurEquipment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedById",
                table: "OurEquipment",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedToId",
                table: "OurEquipment",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OurEquipment_AssignedById",
                table: "OurEquipment",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_OurEquipment_AssignedToId",
                table: "OurEquipment",
                column: "AssignedToId");

            migrationBuilder.AddForeignKey(
                name: "FK_OurEquipment_AspNetUsers_AssignedById",
                table: "OurEquipment",
                column: "AssignedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OurEquipment_AspNetUsers_AssignedToId",
                table: "OurEquipment",
                column: "AssignedToId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OurEquipment_AspNetUsers_AssignedById",
                table: "OurEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_OurEquipment_AspNetUsers_AssignedToId",
                table: "OurEquipment");

            migrationBuilder.DropIndex(
                name: "IX_OurEquipment_AssignedById",
                table: "OurEquipment");

            migrationBuilder.DropIndex(
                name: "IX_OurEquipment_AssignedToId",
                table: "OurEquipment");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "OurEquipment");

            migrationBuilder.DropColumn(
                name: "AssignedById",
                table: "OurEquipment");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "OurEquipment");
        }
    }
}
