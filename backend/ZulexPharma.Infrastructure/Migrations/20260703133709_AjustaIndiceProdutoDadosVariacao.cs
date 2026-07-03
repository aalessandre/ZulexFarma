using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjustaIndiceProdutoDadosVariacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProdutosDados_ProdutoId_FilialId",
                table: "ProdutosDados");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_ProdutoId_FilialId_ProdutoVariacaoId",
                table: "ProdutosDados",
                columns: new[] { "ProdutoId", "FilialId", "ProdutoVariacaoId" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProdutosDados_ProdutoId_FilialId_ProdutoVariacaoId",
                table: "ProdutosDados");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_ProdutoId_FilialId",
                table: "ProdutosDados",
                columns: new[] { "ProdutoId", "FilialId" },
                unique: true);
        }
    }
}
