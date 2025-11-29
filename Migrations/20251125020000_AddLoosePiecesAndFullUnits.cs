using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeuerwerkLager.Migrations
{
    /// <inheritdoc />
    public partial class AddLoosePiecesAndFullUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Entferne alte Constraint auf Quantity
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockEntries_Quantity_NonNegative",
                table: "StockEntries");

            migrationBuilder.AddColumn<int>(
                name: "FullUnits",
                table: "StockEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LoosePieces",
                table: "StockEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Bestehende Daten migrieren: Quantity -> FullUnits, LoosePieces = 0
            migrationBuilder.Sql("UPDATE StockEntries SET FullUnits = Quantity, LoosePieces = 0;");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "StockEntries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockEntries_FullUnits_NonNegative",
                table: "StockEntries",
                sql: "[FullUnits] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockEntries_LoosePieces_NonNegative",
                table: "StockEntries",
                sql: "[LoosePieces] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockEntries_FullUnits_NonNegative",
                table: "StockEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockEntries_LoosePieces_NonNegative",
                table: "StockEntries");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "StockEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Rückmigration: FullUnits + LoosePieces -> Quantity
            migrationBuilder.Sql("UPDATE StockEntries SET Quantity = COALESCE(FullUnits,0) + COALESCE(LoosePieces,0);");

            migrationBuilder.DropColumn(
                name: "FullUnits",
                table: "StockEntries");

            migrationBuilder.DropColumn(
                name: "LoosePieces",
                table: "StockEntries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockEntries_Quantity_NonNegative",
                table: "StockEntries",
                sql: "[Quantity] >= 0");
        }
    }
}
