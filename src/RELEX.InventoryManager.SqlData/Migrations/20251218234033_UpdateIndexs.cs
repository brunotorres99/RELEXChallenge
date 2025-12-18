using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RELEX.InventoryManager.SqlData.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIndexs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Order_LocationCode",
                schema: "inv",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Order_ProductCode",
                schema: "inv",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Order_OrderDate_LocationCode",
                schema: "inv",
                table: "Orders",
                columns: new[] { "OrderDate", "LocationCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Order_OrderDate_ProductCode",
                schema: "inv",
                table: "Orders",
                columns: new[] { "OrderDate", "ProductCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Order_OrderDate_LocationCode",
                schema: "inv",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Order_OrderDate_ProductCode",
                schema: "inv",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Order_LocationCode",
                schema: "inv",
                table: "Orders",
                column: "LocationCode");

            migrationBuilder.CreateIndex(
                name: "IX_Order_ProductCode",
                schema: "inv",
                table: "Orders",
                column: "ProductCode");
        }
    }
}
