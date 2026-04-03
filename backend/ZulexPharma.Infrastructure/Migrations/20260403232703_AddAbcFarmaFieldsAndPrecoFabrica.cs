using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbcFarmaFieldsAndPrecoFabrica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AtualizarAbcFarma",
                table: "SubGrupos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AtualizarAbcFarma",
                table: "Secoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoFabrica",
                table: "ProdutosDados",
                type: "numeric(10,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AtualizarAbcFarma",
                table: "GruposProdutos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AtualizarAbcFarma",
                table: "GruposPrincipais",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtualizarAbcFarma",
                table: "SubGrupos");

            migrationBuilder.DropColumn(
                name: "AtualizarAbcFarma",
                table: "Secoes");

            migrationBuilder.DropColumn(
                name: "PrecoFabrica",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "AtualizarAbcFarma",
                table: "GruposProdutos");

            migrationBuilder.DropColumn(
                name: "AtualizarAbcFarma",
                table: "GruposPrincipais");
        }
    }
}
