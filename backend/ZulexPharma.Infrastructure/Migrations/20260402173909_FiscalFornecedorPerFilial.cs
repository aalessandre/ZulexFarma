using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FiscalFornecedorPerFilial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProdutosFornecedores_ProdutoId",
                table: "ProdutosFornecedores");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosFiscal_ProdutoId",
                table: "ProdutosFiscal");

            migrationBuilder.AddColumn<long>(
                name: "FilialId",
                table: "ProdutosFornecedores",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialId",
                table: "ProdutosFiscal",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFornecedores_ProdutoId_FilialId_FornecedorId",
                table: "ProdutosFornecedores",
                columns: new[] { "ProdutoId", "FilialId", "FornecedorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFiscal_ProdutoId_FilialId",
                table: "ProdutosFiscal",
                columns: new[] { "ProdutoId", "FilialId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProdutosFornecedores_ProdutoId_FilialId_FornecedorId",
                table: "ProdutosFornecedores");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosFiscal_ProdutoId_FilialId",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "FilialId",
                table: "ProdutosFornecedores");

            migrationBuilder.DropColumn(
                name: "FilialId",
                table: "ProdutosFiscal");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFornecedores_ProdutoId",
                table: "ProdutosFornecedores",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFiscal_ProdutoId",
                table: "ProdutosFiscal",
                column: "ProdutoId",
                unique: true);
        }
    }
}
