using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseUserManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseInvoiceAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "PurchaseInvoices",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "PurchaseInvoices",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "PurchaseInvoices");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "PurchaseInvoices");
        }
    }
}
