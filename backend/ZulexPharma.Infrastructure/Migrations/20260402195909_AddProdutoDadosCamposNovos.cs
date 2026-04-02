using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoDadosCamposNovos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstoqueDeposito",
                table: "ProdutosDados",
                type: "numeric(10,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Pmc",
                table: "ProdutosDados",
                type: "numeric(10,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UltimaCompraFrete",
                table: "ProdutosDados",
                type: "numeric(10,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPromocaoPrazo",
                table: "ProdutosDados",
                type: "numeric(10,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstoqueDeposito",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "Pmc",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "UltimaCompraFrete",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "ValorPromocaoPrazo",
                table: "ProdutosDados");
        }
    }
}
