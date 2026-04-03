using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplacesPriorizarWithFormacaoPreco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priorizar",
                table: "SubGrupos");

            migrationBuilder.DropColumn(
                name: "Priorizar",
                table: "Secoes");

            migrationBuilder.DropColumn(
                name: "Priorizar",
                table: "GruposProdutos");

            migrationBuilder.DropColumn(
                name: "Priorizar",
                table: "GruposPrincipais");

            migrationBuilder.AddColumn<string>(
                name: "BaseCalculo",
                table: "SubGrupos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CUSTO_COMPRA");

            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "SubGrupos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MARKUP");

            migrationBuilder.AddColumn<string>(
                name: "BaseCalculo",
                table: "Secoes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CUSTO_COMPRA");

            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "Secoes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MARKUP");

            migrationBuilder.AddColumn<string>(
                name: "BaseCalculo",
                table: "ProdutosDados",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "ProdutosDados",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseCalculo",
                table: "GruposProdutos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CUSTO_COMPRA");

            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "GruposProdutos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MARKUP");

            migrationBuilder.AddColumn<string>(
                name: "BaseCalculo",
                table: "GruposPrincipais",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CUSTO_COMPRA");

            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "GruposPrincipais",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MARKUP");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseCalculo",
                table: "SubGrupos");

            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "SubGrupos");

            migrationBuilder.DropColumn(
                name: "BaseCalculo",
                table: "Secoes");

            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "Secoes");

            migrationBuilder.DropColumn(
                name: "BaseCalculo",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "BaseCalculo",
                table: "GruposProdutos");

            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "GruposProdutos");

            migrationBuilder.DropColumn(
                name: "BaseCalculo",
                table: "GruposPrincipais");

            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "GruposPrincipais");

            migrationBuilder.AddColumn<string>(
                name: "Priorizar",
                table: "SubGrupos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priorizar",
                table: "Secoes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priorizar",
                table: "GruposProdutos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priorizar",
                table: "GruposPrincipais",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
