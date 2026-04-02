using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfiguracaoBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Configuracoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "Configuracoes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Configuracoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Configuracoes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "Configuracoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Configuracoes",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_Codigo",
                table: "Configuracoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_SyncGuid",
                table: "Configuracoes",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Configuracoes_Codigo",
                table: "Configuracoes");

            migrationBuilder.DropIndex(
                name: "IX_Configuracoes_SyncGuid",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Configuracoes");
        }
    }
}
