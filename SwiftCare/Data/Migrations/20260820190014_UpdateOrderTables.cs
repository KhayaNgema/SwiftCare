using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwiftCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackageOrders_Orders_OrderId",
                table: "PackageOrders");

            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "PackageOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderItemId",
                table: "PackageOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PackageOrders_OrderId1",
                table: "PackageOrders",
                column: "OrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_PackageOrders_OrderItemId",
                table: "PackageOrders",
                column: "OrderItemId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageOrders_OrderItems_OrderItemId",
                table: "PackageOrders",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "OrderItemId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageOrders_Orders_OrderId",
                table: "PackageOrders",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageOrders_Orders_OrderId1",
                table: "PackageOrders",
                column: "OrderId1",
                principalTable: "Orders",
                principalColumn: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackageOrders_OrderItems_OrderItemId",
                table: "PackageOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageOrders_Orders_OrderId",
                table: "PackageOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageOrders_Orders_OrderId1",
                table: "PackageOrders");

            migrationBuilder.DropIndex(
                name: "IX_PackageOrders_OrderId1",
                table: "PackageOrders");

            migrationBuilder.DropIndex(
                name: "IX_PackageOrders_OrderItemId",
                table: "PackageOrders");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "PackageOrders");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "PackageOrders");

            migrationBuilder.AddForeignKey(
                name: "FK_PackageOrders_Orders_OrderId",
                table: "PackageOrders",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
