using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeuerwerkLager.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_StockEntries_Quantity_NonNegative",
                table: "StockEntries",
                sql: "[Quantity] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockEntries_Quantity_NonNegative",
                table: "StockEntries");
        }
    }
}
