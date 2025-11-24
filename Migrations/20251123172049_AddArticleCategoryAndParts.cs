using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeuerwerkLager.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleCategoryAndParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Articles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsMultiPart",
                table: "Articles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PiecesPerUnit",
                table: "Articles",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "IsMultiPart",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "PiecesPerUnit",
                table: "Articles");
        }
    }
}
