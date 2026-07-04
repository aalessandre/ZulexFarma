using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoPesavelEQuantidadeDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Quantidade",
                table: "VendaItens",
                type: "numeric(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CodigoBalanca",
                table: "Produtos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Pesavel",
                table: "Produtos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Unidade",
                table: "Produtos",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "UN");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_CodigoBalanca",
                table: "Produtos",
                column: "CodigoBalanca",
                unique: true,
                filter: "\"CodigoBalanca\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Produtos_CodigoBalanca",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "CodigoBalanca",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Pesavel",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Unidade",
                table: "Produtos");

            migrationBuilder.AlterColumn<int>(
                name: "Quantidade",
                table: "VendaItens",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,3)",
                oldPrecision: 10,
                oldScale: 3);
        }
    }
}
