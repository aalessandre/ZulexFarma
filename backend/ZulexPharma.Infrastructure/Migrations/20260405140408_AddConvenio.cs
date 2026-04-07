using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConvenio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Convenios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PessoaId = table.Column<long>(type: "bigint", nullable: false),
                    Aviso = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ModoFechamento = table.Column<int>(type: "integer", nullable: false),
                    DiasCorridos = table.Column<int>(type: "integer", nullable: true),
                    DiaFechamento = table.Column<int>(type: "integer", nullable: true),
                    DiaVencimento = table.Column<int>(type: "integer", nullable: true),
                    MesesParaVencimento = table.Column<int>(type: "integer", nullable: false),
                    QtdeViasCupom = table.Column<int>(type: "integer", nullable: false),
                    Bloqueado = table.Column<bool>(type: "boolean", nullable: false),
                    BloquearVendaParcelada = table.Column<bool>(type: "boolean", nullable: false),
                    BloquearDescontoParcelada = table.Column<bool>(type: "boolean", nullable: false),
                    BloquearComissao = table.Column<bool>(type: "boolean", nullable: false),
                    VenderSomenteComSenha = table.Column<bool>(type: "boolean", nullable: false),
                    CobrarJurosAtraso = table.Column<bool>(type: "boolean", nullable: false),
                    DiasCarenciaBloqueio = table.Column<int>(type: "integer", nullable: false),
                    LimiteCredito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximoParcelas = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convenios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Convenios_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConvenioBloqueios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConvenioId = table.Column<long>(type: "bigint", nullable: false),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvenioBloqueios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConvenioBloqueios_Convenios_ConvenioId",
                        column: x => x.ConvenioId,
                        principalTable: "Convenios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConvenioBloqueios_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConvenioDescontos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConvenioId = table.Column<long>(type: "bigint", nullable: false),
                    TipoAgrupador = table.Column<int>(type: "integer", nullable: false),
                    AgrupadorId = table.Column<long>(type: "bigint", nullable: false),
                    AgrupadorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DescontoMaxSemSenha = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DescontoMaxComSenha = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvenioDescontos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConvenioDescontos_Convenios_ConvenioId",
                        column: x => x.ConvenioId,
                        principalTable: "Convenios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConvenioBloqueios_ConvenioId_TipoPagamentoId",
                table: "ConvenioBloqueios",
                columns: new[] { "ConvenioId", "TipoPagamentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConvenioBloqueios_TipoPagamentoId",
                table: "ConvenioBloqueios",
                column: "TipoPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvenioDescontos_ConvenioId",
                table: "ConvenioDescontos",
                column: "ConvenioId");

            migrationBuilder.CreateIndex(
                name: "IX_Convenios_Codigo",
                table: "Convenios",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Convenios_PessoaId",
                table: "Convenios",
                column: "PessoaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Convenios_SyncGuid",
                table: "Convenios",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConvenioBloqueios");

            migrationBuilder.DropTable(
                name: "ConvenioDescontos");

            migrationBuilder.DropTable(
                name: "Convenios");
        }
    }
}
