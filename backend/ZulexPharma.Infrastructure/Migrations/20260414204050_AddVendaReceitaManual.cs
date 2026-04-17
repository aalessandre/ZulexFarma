using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendaReceitaManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendaReceitaItens_VendaItens_VendaItemId",
                table: "VendaReceitaItens");

            migrationBuilder.DropForeignKey(
                name: "FK_VendaReceitas_Vendas_VendaId",
                table: "VendaReceitas");

            migrationBuilder.AlterColumn<long>(
                name: "VendaId",
                table: "VendaReceitas",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "FilialId",
                table: "VendaReceitas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "VendaItemId",
                table: "VendaReceitaItens",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "ProdutoId",
                table: "VendaReceitaItens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitas_FilialId",
                table: "VendaReceitas",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_ProdutoId",
                table: "VendaReceitaItens",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendaReceitaItens_Produtos_ProdutoId",
                table: "VendaReceitaItens",
                column: "ProdutoId",
                principalTable: "Produtos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VendaReceitaItens_VendaItens_VendaItemId",
                table: "VendaReceitaItens",
                column: "VendaItemId",
                principalTable: "VendaItens",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_VendaReceitas_Filiais_FilialId",
                table: "VendaReceitas",
                column: "FilialId",
                principalTable: "Filiais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VendaReceitas_Vendas_VendaId",
                table: "VendaReceitas",
                column: "VendaId",
                principalTable: "Vendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendaReceitaItens_Produtos_ProdutoId",
                table: "VendaReceitaItens");

            migrationBuilder.DropForeignKey(
                name: "FK_VendaReceitaItens_VendaItens_VendaItemId",
                table: "VendaReceitaItens");

            migrationBuilder.DropForeignKey(
                name: "FK_VendaReceitas_Filiais_FilialId",
                table: "VendaReceitas");

            migrationBuilder.DropForeignKey(
                name: "FK_VendaReceitas_Vendas_VendaId",
                table: "VendaReceitas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitas_FilialId",
                table: "VendaReceitas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitaItens_ProdutoId",
                table: "VendaReceitaItens");

            migrationBuilder.DropColumn(
                name: "FilialId",
                table: "VendaReceitas");

            migrationBuilder.DropColumn(
                name: "ProdutoId",
                table: "VendaReceitaItens");

            migrationBuilder.AlterColumn<long>(
                name: "VendaId",
                table: "VendaReceitas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "VendaItemId",
                table: "VendaReceitaItens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VendaReceitaItens_VendaItens_VendaItemId",
                table: "VendaReceitaItens",
                column: "VendaItemId",
                principalTable: "VendaItens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VendaReceitas_Vendas_VendaId",
                table: "VendaReceitas",
                column: "VendaId",
                principalTable: "Vendas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
