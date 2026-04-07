using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromocao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Promocoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    DataHoraInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataHoraFim = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    PermitirMudarPreco = table.Column<bool>(type: "boolean", nullable: false),
                    GerarComissao = table.Column<bool>(type: "boolean", nullable: false),
                    ExclusivaConvenio = table.Column<bool>(type: "boolean", nullable: false),
                    ReducaoVendaPrazo = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    QtdeMaxPorVenda = table.Column<int>(type: "integer", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promocoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromocaoConvenios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromocaoId = table.Column<long>(type: "bigint", nullable: false),
                    ConvenioId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocaoConvenios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocaoConvenios_Convenios_ConvenioId",
                        column: x => x.ConvenioId,
                        principalTable: "Convenios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromocaoConvenios_Promocoes_PromocaoId",
                        column: x => x.PromocaoId,
                        principalTable: "Promocoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromocaoFiliais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromocaoId = table.Column<long>(type: "bigint", nullable: false),
                    FilialId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocaoFiliais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocaoFiliais_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromocaoFiliais_Promocoes_PromocaoId",
                        column: x => x.PromocaoId,
                        principalTable: "Promocoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromocaoPagamentos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromocaoId = table.Column<long>(type: "bigint", nullable: false),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocaoPagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocaoPagamentos_Promocoes_PromocaoId",
                        column: x => x.PromocaoId,
                        principalTable: "Promocoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromocaoPagamentos_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromocaoProdutos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromocaoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    PercentualPromocao = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    ValorPromocao = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentualLucro = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    LancarPorQuantidade = table.Column<bool>(type: "boolean", nullable: false),
                    QtdeLimite = table.Column<int>(type: "integer", nullable: true),
                    QtdeVendida = table.Column<int>(type: "integer", nullable: false),
                    DataInicioContagem = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PercentualAposLimite = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    ValorAposLimite = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocaoProdutos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocaoProdutos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromocaoProdutos_Promocoes_PromocaoId",
                        column: x => x.PromocaoId,
                        principalTable: "Promocoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromocaoConvenios_ConvenioId",
                table: "PromocaoConvenios",
                column: "ConvenioId");

            migrationBuilder.CreateIndex(
                name: "IX_PromocaoConvenios_PromocaoId",
                table: "PromocaoConvenios",
                column: "PromocaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PromocaoFiliais_FilialId",
                table: "PromocaoFiliais",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_PromocaoFiliais_PromocaoId",
                table: "PromocaoFiliais",
                column: "PromocaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PromocaoPagamentos_PromocaoId",
                table: "PromocaoPagamentos",
                column: "PromocaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PromocaoPagamentos_TipoPagamentoId",
                table: "PromocaoPagamentos",
                column: "TipoPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PromocaoProdutos_ProdutoId",
                table: "PromocaoProdutos",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_PromocaoProdutos_PromocaoId",
                table: "PromocaoProdutos",
                column: "PromocaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Promocoes_Codigo",
                table: "Promocoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Promocoes_SyncGuid",
                table: "Promocoes",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromocaoConvenios");

            migrationBuilder.DropTable(
                name: "PromocaoFiliais");

            migrationBuilder.DropTable(
                name: "PromocaoPagamentos");

            migrationBuilder.DropTable(
                name: "PromocaoProdutos");

            migrationBuilder.DropTable(
                name: "Promocoes");
        }
    }
}
