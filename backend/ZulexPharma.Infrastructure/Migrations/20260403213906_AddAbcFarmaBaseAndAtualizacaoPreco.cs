using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbcFarmaBaseAndAtualizacaoPreco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbcFarmaBase",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ean = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    RegistroAnvisa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Composicao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NomeFabricante = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClasseTerapeutica = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Ncm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Pf0 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc0 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf12 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc12 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf17 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc17 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf18 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc18 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf19 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc19 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf195 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc195 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf20 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc20 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf205 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc205 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf21 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc21 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf22 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc22 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf225 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc225 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pf23 = table.Column<decimal>(type: "numeric", nullable: false),
                    Pmc23 = table.Column<decimal>(type: "numeric", nullable: false),
                    PercentualIpi = table.Column<decimal>(type: "numeric", nullable: false),
                    DataVigencia = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbcFarmaBase", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AtualizacoesPreco",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DataExecucao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    NomeUsuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FiltroJson = table.Column<string>(type: "text", nullable: true),
                    TotalProdutos = table.Column<int>(type: "integer", nullable: false),
                    TotalAlterados = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtualizacoesPreco", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AtualizacoesPrecoItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AtualizacaoPrecoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoDadosId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ValorVendaAnterior = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorVendaNovo = table.Column<decimal>(type: "numeric", nullable: false),
                    PmcAnterior = table.Column<decimal>(type: "numeric", nullable: false),
                    PmcNovo = table.Column<decimal>(type: "numeric", nullable: false),
                    CustoMedioAnterior = table.Column<decimal>(type: "numeric", nullable: false),
                    MarkupAnterior = table.Column<decimal>(type: "numeric", nullable: false),
                    ProjecaoLucroAnterior = table.Column<decimal>(type: "numeric", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtualizacoesPrecoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AtualizacoesPrecoItens_AtualizacoesPreco_AtualizacaoPrecoId",
                        column: x => x.AtualizacaoPrecoId,
                        principalTable: "AtualizacoesPreco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbcFarmaBase_Ean",
                table: "AbcFarmaBase",
                column: "Ean");

            migrationBuilder.CreateIndex(
                name: "IX_AbcFarmaBase_RegistroAnvisa",
                table: "AbcFarmaBase",
                column: "RegistroAnvisa");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPreco_Codigo",
                table: "AtualizacoesPreco",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPreco_FilialId",
                table: "AtualizacoesPreco",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPreco_SyncGuid",
                table: "AtualizacoesPreco",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPrecoItens_AtualizacaoPrecoId",
                table: "AtualizacoesPrecoItens",
                column: "AtualizacaoPrecoId");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPrecoItens_Codigo",
                table: "AtualizacoesPrecoItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPrecoItens_SyncGuid",
                table: "AtualizacoesPrecoItens",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbcFarmaBase");

            migrationBuilder.DropTable(
                name: "AtualizacoesPrecoItens");

            migrationBuilder.DropTable(
                name: "AtualizacoesPreco");
        }
    }
}
