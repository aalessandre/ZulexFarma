using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreVendas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true),
                    ColaboradorId = table.Column<long>(type: "bigint", nullable: true),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: true),
                    ConvenioId = table.Column<long>(type: "bigint", nullable: true),
                    TotalBruto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDesconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalLiquido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalItens = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PreVendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreVendas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PreVendas_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PreVendas_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreVendas_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PreVendaItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PreVendaId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoCodigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProdutoNome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Fabricante = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PrecoVenda = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PercentualDesconto = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreVendaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreVendaItens_PreVendas_PreVendaId",
                        column: x => x.PreVendaId,
                        principalTable: "PreVendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreVendaItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreVendaItens_PreVendaId",
                table: "PreVendaItens",
                column: "PreVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendaItens_ProdutoId",
                table: "PreVendaItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_ClienteId",
                table: "PreVendas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_Codigo",
                table: "PreVendas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_ColaboradorId",
                table: "PreVendas",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_FilialId",
                table: "PreVendas",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_Status",
                table: "PreVendas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_SyncGuid",
                table: "PreVendas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_TipoPagamentoId",
                table: "PreVendas",
                column: "TipoPagamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreVendaItens");

            migrationBuilder.DropTable(
                name: "PreVendas");
        }
    }
}
