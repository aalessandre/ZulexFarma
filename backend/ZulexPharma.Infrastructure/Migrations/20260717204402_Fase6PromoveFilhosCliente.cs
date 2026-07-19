using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase6PromoveFilhosCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClienteBloqueios_TiposPagamento_TipoPagamentoId",
                table: "ClienteBloqueios");

            migrationBuilder.DropForeignKey(
                name: "FK_ClienteConvenios_Convenios_ConvenioId",
                table: "ClienteConvenios");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "ClienteUsosContinuos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "ClienteUsosContinuos",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ClienteUsosContinuos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "ClienteUsosContinuos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "ClienteUsosContinuos",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "NoOrigemId",
                table: "ClienteUsosContinuos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ClienteUsosContinuos",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "ClienteDescontos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "ClienteDescontos",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ClienteDescontos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "ClienteDescontos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "ClienteDescontos",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "NoOrigemId",
                table: "ClienteDescontos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ClienteDescontos",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "ClienteConvenios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "ClienteConvenios",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ClienteConvenios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "ClienteConvenios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "ClienteConvenios",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "NoOrigemId",
                table: "ClienteConvenios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ClienteConvenios",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "ClienteBloqueios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "ClienteBloqueios",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ClienteBloqueios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "ClienteBloqueios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "ClienteBloqueios",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "NoOrigemId",
                table: "ClienteBloqueios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ClienteBloqueios",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "ClienteAutorizacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "ClienteAutorizacoes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ClienteAutorizacoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "ClienteAutorizacoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "ClienteAutorizacoes",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "NoOrigemId",
                table: "ClienteAutorizacoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ClienteAutorizacoes",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteUsosContinuos_Codigo_NoOrigemId",
                table: "ClienteUsosContinuos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteUsosContinuos_SyncGuid",
                table: "ClienteUsosContinuos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClienteDescontos_Codigo_NoOrigemId",
                table: "ClienteDescontos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteDescontos_SyncGuid",
                table: "ClienteDescontos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClienteConvenios_Codigo_NoOrigemId",
                table: "ClienteConvenios",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteConvenios_SyncGuid",
                table: "ClienteConvenios",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClienteBloqueios_Codigo_NoOrigemId",
                table: "ClienteBloqueios",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteBloqueios_SyncGuid",
                table: "ClienteBloqueios",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClienteAutorizacoes_Codigo_NoOrigemId",
                table: "ClienteAutorizacoes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteAutorizacoes_SyncGuid",
                table: "ClienteAutorizacoes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClienteBloqueios_TiposPagamento_TipoPagamentoId",
                table: "ClienteBloqueios",
                column: "TipoPagamentoId",
                principalTable: "TiposPagamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClienteConvenios_Convenios_ConvenioId",
                table: "ClienteConvenios",
                column: "ConvenioId",
                principalTable: "Convenios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClienteBloqueios_TiposPagamento_TipoPagamentoId",
                table: "ClienteBloqueios");

            migrationBuilder.DropForeignKey(
                name: "FK_ClienteConvenios_Convenios_ConvenioId",
                table: "ClienteConvenios");

            migrationBuilder.DropIndex(
                name: "IX_ClienteUsosContinuos_Codigo_NoOrigemId",
                table: "ClienteUsosContinuos");

            migrationBuilder.DropIndex(
                name: "IX_ClienteUsosContinuos_SyncGuid",
                table: "ClienteUsosContinuos");

            migrationBuilder.DropIndex(
                name: "IX_ClienteDescontos_Codigo_NoOrigemId",
                table: "ClienteDescontos");

            migrationBuilder.DropIndex(
                name: "IX_ClienteDescontos_SyncGuid",
                table: "ClienteDescontos");

            migrationBuilder.DropIndex(
                name: "IX_ClienteConvenios_Codigo_NoOrigemId",
                table: "ClienteConvenios");

            migrationBuilder.DropIndex(
                name: "IX_ClienteConvenios_SyncGuid",
                table: "ClienteConvenios");

            migrationBuilder.DropIndex(
                name: "IX_ClienteBloqueios_Codigo_NoOrigemId",
                table: "ClienteBloqueios");

            migrationBuilder.DropIndex(
                name: "IX_ClienteBloqueios_SyncGuid",
                table: "ClienteBloqueios");

            migrationBuilder.DropIndex(
                name: "IX_ClienteAutorizacoes_Codigo_NoOrigemId",
                table: "ClienteAutorizacoes");

            migrationBuilder.DropIndex(
                name: "IX_ClienteAutorizacoes_SyncGuid",
                table: "ClienteAutorizacoes");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "ClienteUsosContinuos");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "ClienteUsosContinuos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ClienteUsosContinuos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "ClienteUsosContinuos");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "ClienteUsosContinuos");

            migrationBuilder.DropColumn(
                name: "NoOrigemId",
                table: "ClienteUsosContinuos");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ClienteUsosContinuos");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "ClienteDescontos");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "ClienteDescontos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ClienteDescontos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "ClienteDescontos");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "ClienteDescontos");

            migrationBuilder.DropColumn(
                name: "NoOrigemId",
                table: "ClienteDescontos");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ClienteDescontos");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "ClienteConvenios");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "ClienteConvenios");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ClienteConvenios");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "ClienteConvenios");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "ClienteConvenios");

            migrationBuilder.DropColumn(
                name: "NoOrigemId",
                table: "ClienteConvenios");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ClienteConvenios");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "ClienteBloqueios");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "ClienteBloqueios");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ClienteBloqueios");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "ClienteBloqueios");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "ClienteBloqueios");

            migrationBuilder.DropColumn(
                name: "NoOrigemId",
                table: "ClienteBloqueios");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ClienteBloqueios");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "ClienteAutorizacoes");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "ClienteAutorizacoes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ClienteAutorizacoes");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "ClienteAutorizacoes");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "ClienteAutorizacoes");

            migrationBuilder.DropColumn(
                name: "NoOrigemId",
                table: "ClienteAutorizacoes");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ClienteAutorizacoes");

            migrationBuilder.AddForeignKey(
                name: "FK_ClienteBloqueios_TiposPagamento_TipoPagamentoId",
                table: "ClienteBloqueios",
                column: "TipoPagamentoId",
                principalTable: "TiposPagamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClienteConvenios_Convenios_ConvenioId",
                table: "ClienteConvenios",
                column: "ConvenioId",
                principalTable: "Convenios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
