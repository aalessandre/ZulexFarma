using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropReceitasOrfas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceitasItens");

            migrationBuilder.DropTable(
                name: "Receitas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Receitas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaId = table.Column<long>(type: "bigint", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    MedicoCpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    MedicoCrm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MedicoNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MedicoUf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    NumeroReceita = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PacienteCep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PacienteCidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PacienteCpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    PacienteEndereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PacienteNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PacienteUf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TipoReceita = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receitas_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReceitasItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoLoteId = table.Column<long>(type: "bigint", nullable: true),
                    ReceitaId = table.Column<long>(type: "bigint", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    Posologia = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceitasItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceitasItens_ProdutosLotes_ProdutoLoteId",
                        column: x => x.ProdutoLoteId,
                        principalTable: "ProdutosLotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReceitasItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceitasItens_Receitas_ReceitaId",
                        column: x => x.ReceitaId,
                        principalTable: "Receitas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_Codigo",
                table: "Receitas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_FilialId_DataEmissao",
                table: "Receitas",
                columns: new[] { "FilialId", "DataEmissao" });

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_SyncGuid",
                table: "Receitas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_VendaId",
                table: "Receitas",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_Codigo",
                table: "ReceitasItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_ProdutoId",
                table: "ReceitasItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_ProdutoLoteId",
                table: "ReceitasItens",
                column: "ProdutoLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_ReceitaId",
                table: "ReceitasItens",
                column: "ReceitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_SyncGuid",
                table: "ReceitasItens",
                column: "SyncGuid");
        }
    }
}
