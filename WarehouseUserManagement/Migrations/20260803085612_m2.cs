using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseUserManagement.Migrations
{
    /// <inheritdoc />
    public partial class m2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OldInvoiceArchives",
                columns: table => new
                {
                    OldInvoiceArchiveId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoredPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SupplierNameNote = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    InvoiceDateNote = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldInvoiceArchives", x => x.OldInvoiceArchiveId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OldInvoiceArchives_UploadedDate",
                table: "OldInvoiceArchives",
                column: "UploadedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OldInvoiceArchives");
        }
    }
}
