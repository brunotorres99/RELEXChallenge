using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RELEX.InventoryManager.SqlData.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inv");

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    ProductCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    SubmittedBy = table.Column<string>(type: "varchar(100)", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_LocationCode",
                schema: "inv",
                table: "Orders",
                column: "LocationCode");

            migrationBuilder.CreateIndex(
                name: "IX_Order_OrderDate",
                schema: "inv",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Order_ProductCode",
                schema: "inv",
                table: "Orders",
                column: "ProductCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders",
                schema: "inv");
        }
    }
}
