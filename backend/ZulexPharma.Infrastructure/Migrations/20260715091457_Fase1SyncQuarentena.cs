using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase1SyncQuarentena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncQuarentena",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Operacao = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: false),
                    DadosJson = table.Column<string>(type: "text", nullable: true),
                    OpCriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NoOrigemId = table.Column<long>(type: "bigint", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Tentativas = table.Column<int>(type: "integer", nullable: false),
                    UltimoErro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Resolvido = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncQuarentena", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncQuarentena_Resolvido",
                table: "SyncQuarentena",
                column: "Resolvido");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQuarentena_Tabela_RegistroId_Operacao",
                table: "SyncQuarentena",
                columns: new[] { "Tabela", "RegistroId", "Operacao" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncQuarentena");
        }
    }
}
