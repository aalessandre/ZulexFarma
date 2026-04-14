using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubstanciaPortariaTipoReceita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Adendo",
                table: "Substancias",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TipoReceita",
                table: "Substancias",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValidadeReceitaDias",
                table: "Substancias",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adendo",
                table: "Substancias");

            migrationBuilder.DropColumn(
                name: "TipoReceita",
                table: "Substancias");

            migrationBuilder.DropColumn(
                name: "ValidadeReceitaDias",
                table: "Substancias");
        }
    }
}
