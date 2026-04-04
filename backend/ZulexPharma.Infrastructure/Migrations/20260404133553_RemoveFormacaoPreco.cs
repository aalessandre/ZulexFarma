using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFormacaoPreco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "SubGrupos");

            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "Secoes");

            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "GruposProdutos");

            migrationBuilder.DropColumn(
                name: "FormacaoPreco",
                table: "GruposPrincipais");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "SubGrupos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MARKUP");

            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "Secoes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MARKUP");

            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "ProdutosDados",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "GruposProdutos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MARKUP");

            migrationBuilder.AddColumn<string>(
                name: "FormacaoPreco",
                table: "GruposPrincipais",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "MARKUP");
        }
    }
}
