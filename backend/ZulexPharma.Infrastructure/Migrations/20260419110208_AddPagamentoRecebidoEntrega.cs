using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPagamentoRecebidoEntrega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CaixaRecebimentoId",
                table: "Vendas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataPagamentoRecebido",
                table: "Vendas",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EntregaEnderecoId",
                table: "Vendas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaObservacao",
                table: "Vendas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EntregaSolicitada",
                table: "Vendas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PagamentoRecebido",
                table: "Vendas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_CaixaRecebimentoId",
                table: "Vendas",
                column: "CaixaRecebimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_EntregaEnderecoId",
                table: "Vendas",
                column: "EntregaEnderecoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_Caixas_CaixaRecebimentoId",
                table: "Vendas",
                column: "CaixaRecebimentoId",
                principalTable: "Caixas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_PessoasEndereco_EntregaEnderecoId",
                table: "Vendas",
                column: "EntregaEnderecoId",
                principalTable: "PessoasEndereco",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_Caixas_CaixaRecebimentoId",
                table: "Vendas");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_PessoasEndereco_EntregaEnderecoId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_CaixaRecebimentoId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_EntregaEnderecoId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "CaixaRecebimentoId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "DataPagamentoRecebido",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "EntregaEnderecoId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "EntregaObservacao",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "EntregaSolicitada",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "PagamentoRecebido",
                table: "Vendas");
        }
    }
}
