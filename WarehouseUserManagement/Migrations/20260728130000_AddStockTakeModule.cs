using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseUserManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTakeModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockTakes",
                columns: table => new
                {
                    StockTakeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTakeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakes", x => x.StockTakeId);
                });

            migrationBuilder.CreateTable(
                name: "StockTakeDetails",
                columns: table => new
                {
                    StockTakeDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTakeId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakeDetails", x => x.StockTakeDetailId);
                    table.ForeignKey(
                        name: "FK_StockTakeDetails_StockTakes_StockTakeId",
                        column: x => x.StockTakeId,
                        principalTable: "StockTakes",
                        principalColumn: "StockTakeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTakeDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTakes_Status",
                table: "StockTakes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakes_StockTakeDate",
                table: "StockTakes",
                column: "StockTakeDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakeDetails_StockTakeId",
                table: "StockTakeDetails",
                column: "StockTakeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakeDetails_ProductId",
                table: "StockTakeDetails",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTakeDetails");

            migrationBuilder.DropTable(
                name: "StockTakes");
        }
    }
}
