using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendaPagamentoCartao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CartaoAutorizacao",
                table: "VendaPagamentos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CartaoBandeira",
                table: "VendaPagamentos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CartaoCnpjCredenciadora",
                table: "VendaPagamentos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CartaoTipo",
                table: "VendaPagamentos",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CartaoAutorizacao",
                table: "VendaPagamentos");

            migrationBuilder.DropColumn(
                name: "CartaoBandeira",
                table: "VendaPagamentos");

            migrationBuilder.DropColumn(
                name: "CartaoCnpjCredenciadora",
                table: "VendaPagamentos");

            migrationBuilder.DropColumn(
                name: "CartaoTipo",
                table: "VendaPagamentos");
        }
    }
}
