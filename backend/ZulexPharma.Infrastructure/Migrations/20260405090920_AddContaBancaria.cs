using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContaBancaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContasBancarias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoConta = table.Column<int>(type: "integer", nullable: false),
                    Banco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Agencia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AgenciaDigito = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    NumeroConta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ContaDigito = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    ChavePix = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SaldoInicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataSaldoInicial = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PlanoContaId = table.Column<long>(type: "bigint", nullable: true),
                    FilialId = table.Column<long>(type: "bigint", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasBancarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasBancarias_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasBancarias_PlanosContas_PlanoContaId",
                        column: x => x.PlanoContaId,
                        principalTable: "PlanosContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_Codigo",
                table: "ContasBancarias",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_FilialId",
                table: "ContasBancarias",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_PlanoContaId",
                table: "ContasBancarias",
                column: "PlanoContaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_SyncGuid",
                table: "ContasBancarias",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContasBancarias");
        }
    }
}
