using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoDadosNavProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_ProdutoFamiliaId",
                table: "ProdutosDados",
                column: "ProdutoFamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_ProdutoLocalId",
                table: "ProdutosDados",
                column: "ProdutoLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_SecaoId",
                table: "ProdutosDados",
                column: "SecaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProdutosDados_ProdutoFamilias_ProdutoFamiliaId",
                table: "ProdutosDados",
                column: "ProdutoFamiliaId",
                principalTable: "ProdutoFamilias",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProdutosDados_ProdutosLocais_ProdutoLocalId",
                table: "ProdutosDados",
                column: "ProdutoLocalId",
                principalTable: "ProdutosLocais",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProdutosDados_Secoes_SecaoId",
                table: "ProdutosDados",
                column: "SecaoId",
                principalTable: "Secoes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProdutosDados_ProdutoFamilias_ProdutoFamiliaId",
                table: "ProdutosDados");

            migrationBuilder.DropForeignKey(
                name: "FK_ProdutosDados_ProdutosLocais_ProdutoLocalId",
                table: "ProdutosDados");

            migrationBuilder.DropForeignKey(
                name: "FK_ProdutosDados_Secoes_SecaoId",
                table: "ProdutosDados");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosDados_ProdutoFamiliaId",
                table: "ProdutosDados");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosDados_ProdutoLocalId",
                table: "ProdutosDados");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosDados_SecaoId",
                table: "ProdutosDados");
        }
    }
}
