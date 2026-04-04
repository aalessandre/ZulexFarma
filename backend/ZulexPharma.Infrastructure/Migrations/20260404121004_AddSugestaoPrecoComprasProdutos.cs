using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSugestaoPrecoComprasProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrecificacaoAplicada",
                table: "ComprasProdutos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SugestaoCustoMedio",
                table: "ComprasProdutos",
                type: "numeric(12,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SugestaoMarkup",
                table: "ComprasProdutos",
                type: "numeric(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SugestaoProjecao",
                table: "ComprasProdutos",
                type: "numeric(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SugestaoVenda",
                table: "ComprasProdutos",
                type: "numeric(12,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecificacaoAplicada",
                table: "ComprasProdutos");

            migrationBuilder.DropColumn(
                name: "SugestaoCustoMedio",
                table: "ComprasProdutos");

            migrationBuilder.DropColumn(
                name: "SugestaoMarkup",
                table: "ComprasProdutos");

            migrationBuilder.DropColumn(
                name: "SugestaoProjecao",
                table: "ComprasProdutos");

            migrationBuilder.DropColumn(
                name: "SugestaoVenda",
                table: "ComprasProdutos");
        }
    }
}
