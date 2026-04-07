using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContaPagar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContasPagar",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FornecedorId = table.Column<long>(type: "bigint", nullable: true),
                    PlanoContaId = table.Column<long>(type: "bigint", nullable: true),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    CompraId = table.Column<long>(type: "bigint", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Juros = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Multa = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorFinal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NrDocumento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NrNotaFiscal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RecorrenciaGrupo = table.Column<Guid>(type: "uuid", nullable: true),
                    RecorrenciaParcela = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasPagar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasPagar_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContasPagar_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasPagar_PlanosContas_PlanoContaId",
                        column: x => x.PlanoContaId,
                        principalTable: "PlanosContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_Codigo",
                table: "ContasPagar",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_DataVencimento",
                table: "ContasPagar",
                column: "DataVencimento");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_FilialId",
                table: "ContasPagar",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_FornecedorId",
                table: "ContasPagar",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_PlanoContaId",
                table: "ContasPagar",
                column: "PlanoContaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_RecorrenciaGrupo",
                table: "ContasPagar",
                column: "RecorrenciaGrupo");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_Status",
                table: "ContasPagar",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_SyncGuid",
                table: "ContasPagar",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContasPagar");
        }
    }
}
