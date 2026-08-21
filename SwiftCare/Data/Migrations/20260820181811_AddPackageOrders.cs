using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageOrders",
                columns: table => new
                {
                    PackageOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PackageNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QrCodeValue = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsReceived = table.Column<bool>(type: "bit", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageOrders", x => x.PackageOrderId);
                    table.ForeignKey(
                        name: "FK_PackageOrders_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageOrders_OrderId",
                table: "PackageOrders",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageOrders_QrCodeValue",
                table: "PackageOrders",
                column: "QrCodeValue",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageOrders");
        }
    }
}
