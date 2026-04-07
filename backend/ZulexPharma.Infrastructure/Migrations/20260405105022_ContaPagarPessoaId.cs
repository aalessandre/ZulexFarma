using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContaPagarPessoaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContasPagar_Fornecedores_FornecedorId",
                table: "ContasPagar");

            migrationBuilder.RenameColumn(
                name: "FornecedorId",
                table: "ContasPagar",
                newName: "PessoaId");

            migrationBuilder.RenameIndex(
                name: "IX_ContasPagar_FornecedorId",
                table: "ContasPagar",
                newName: "IX_ContasPagar_PessoaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContasPagar_Pessoas_PessoaId",
                table: "ContasPagar",
                column: "PessoaId",
                principalTable: "Pessoas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContasPagar_Pessoas_PessoaId",
                table: "ContasPagar");

            migrationBuilder.RenameColumn(
                name: "PessoaId",
                table: "ContasPagar",
                newName: "FornecedorId");

            migrationBuilder.RenameIndex(
                name: "IX_ContasPagar_PessoaId",
                table: "ContasPagar",
                newName: "IX_ContasPagar_FornecedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContasPagar_Fornecedores_FornecedorId",
                table: "ContasPagar",
                column: "FornecedorId",
                principalTable: "Fornecedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
