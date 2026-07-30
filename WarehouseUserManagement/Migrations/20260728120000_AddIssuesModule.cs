using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseUserManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    ReceiverName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancelledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.IssueId);
                    table.ForeignKey(
                        name: "FK_Issues_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueDetails",
                columns: table => new
                {
                    IssueDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueDetails", x => x.IssueDetailId);
                    table.ForeignKey(
                        name: "FK_IssueDetails_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Issues_DepartmentId",
                table: "Issues",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_IssueDate",
                table: "Issues",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_IssueNumber",
                table: "Issues",
                column: "IssueNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Status",
                table: "Issues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IssueDetails_IssueId",
                table: "IssueDetails",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueDetails_ProductId",
                table: "IssueDetails",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueDetails");

            migrationBuilder.DropTable(
                name: "Issues");
        }
    }
}
