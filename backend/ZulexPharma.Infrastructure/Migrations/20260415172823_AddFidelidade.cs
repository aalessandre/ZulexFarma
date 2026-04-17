using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFidelidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampanhasFidelidade",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    ModoContagem = table.Column<int>(type: "integer", nullable: false),
                    ValorBase = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PontosGanhos = table.Column<int>(type: "integer", nullable: false),
                    PercentualCashback = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    FormaRetirada = table.Column<int>(type: "integer", nullable: false),
                    ValorPorPonto = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    DiasValidadePontos = table.Column<int>(type: "integer", nullable: false),
                    LimiarAlerta = table.Column<int>(type: "integer", nullable: false),
                    DataHoraInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataHoraFim = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    HoraFim = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampanhasFidelidade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PremiosFidelidade",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PontosNecessarios = table.Column<int>(type: "integer", nullable: false),
                    ImagemUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Estoque = table.Column<int>(type: "integer", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiosFidelidade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampanhasFidelidadeFiliais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaFidelidadeId = table.Column<long>(type: "bigint", nullable: false),
                    FilialId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampanhasFidelidadeFiliais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeFiliais_CampanhasFidelidade_CampanhaFide~",
                        column: x => x.CampanhaFidelidadeId,
                        principalTable: "CampanhasFidelidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeFiliais_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampanhasFidelidadeItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaFidelidadeId = table.Column<long>(type: "bigint", nullable: false),
                    GrupoPrincipalId = table.Column<long>(type: "bigint", nullable: true),
                    GrupoProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    SubGrupoId = table.Column<long>(type: "bigint", nullable: true),
                    SecaoId = table.Column<long>(type: "bigint", nullable: true),
                    ProdutoFamiliaId = table.Column<long>(type: "bigint", nullable: true),
                    FabricanteId = table.Column<long>(type: "bigint", nullable: true),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    Incluir = table.Column<bool>(type: "boolean", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampanhasFidelidadeItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeItens_CampanhasFidelidade_CampanhaFideli~",
                        column: x => x.CampanhaFidelidadeId,
                        principalTable: "CampanhasFidelidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeItens_Fabricantes_FabricanteId",
                        column: x => x.FabricanteId,
                        principalTable: "Fabricantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeItens_GruposPrincipais_GrupoPrincipalId",
                        column: x => x.GrupoPrincipalId,
                        principalTable: "GruposPrincipais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeItens_GruposProdutos_GrupoProdutoId",
                        column: x => x.GrupoProdutoId,
                        principalTable: "GruposProdutos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeItens_ProdutoFamilias_ProdutoFamiliaId",
                        column: x => x.ProdutoFamiliaId,
                        principalTable: "ProdutoFamilias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeItens_Secoes_SecaoId",
                        column: x => x.SecaoId,
                        principalTable: "Secoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadeItens_SubGrupos_SubGrupoId",
                        column: x => x.SubGrupoId,
                        principalTable: "SubGrupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampanhasFidelidadePagamentos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampanhaFidelidadeId = table.Column<long>(type: "bigint", nullable: false),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampanhasFidelidadePagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadePagamentos_CampanhasFidelidade_CampanhaF~",
                        column: x => x.CampanhaFidelidadeId,
                        principalTable: "CampanhasFidelidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampanhasFidelidadePagamentos_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidade_Codigo",
                table: "CampanhasFidelidade",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidade_SyncGuid",
                table: "CampanhasFidelidade",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidade_Tipo_Ativo",
                table: "CampanhasFidelidade",
                columns: new[] { "Tipo", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeFiliais_CampanhaFidelidadeId_FilialId",
                table: "CampanhasFidelidadeFiliais",
                columns: new[] { "CampanhaFidelidadeId", "FilialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeFiliais_FilialId",
                table: "CampanhasFidelidadeFiliais",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_CampanhaFidelidadeId",
                table: "CampanhasFidelidadeItens",
                column: "CampanhaFidelidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_Codigo",
                table: "CampanhasFidelidadeItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_FabricanteId",
                table: "CampanhasFidelidadeItens",
                column: "FabricanteId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_GrupoPrincipalId",
                table: "CampanhasFidelidadeItens",
                column: "GrupoPrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_GrupoProdutoId",
                table: "CampanhasFidelidadeItens",
                column: "GrupoProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_ProdutoFamiliaId",
                table: "CampanhasFidelidadeItens",
                column: "ProdutoFamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_ProdutoId",
                table: "CampanhasFidelidadeItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_SecaoId",
                table: "CampanhasFidelidadeItens",
                column: "SecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_SubGrupoId",
                table: "CampanhasFidelidadeItens",
                column: "SubGrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_SyncGuid",
                table: "CampanhasFidelidadeItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadePagamentos_CampanhaFidelidadeId_TipoPaga~",
                table: "CampanhasFidelidadePagamentos",
                columns: new[] { "CampanhaFidelidadeId", "TipoPagamentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadePagamentos_TipoPagamentoId",
                table: "CampanhasFidelidadePagamentos",
                column: "TipoPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PremiosFidelidade_Codigo",
                table: "PremiosFidelidade",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PremiosFidelidade_Nome",
                table: "PremiosFidelidade",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_PremiosFidelidade_SyncGuid",
                table: "PremiosFidelidade",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampanhasFidelidadeFiliais");

            migrationBuilder.DropTable(
                name: "CampanhasFidelidadeItens");

            migrationBuilder.DropTable(
                name: "CampanhasFidelidadePagamentos");

            migrationBuilder.DropTable(
                name: "PremiosFidelidade");

            migrationBuilder.DropTable(
                name: "CampanhasFidelidade");
        }
    }
}
