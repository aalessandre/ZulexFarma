using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeVariacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProdutoVariacaoId",
                table: "VendaItens",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProdutoVariacaoId",
                table: "ProdutosDados",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ControlaGrade",
                table: "Produtos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AtributosVariacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtributosVariacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosVariacoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PrecoProprio = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosVariacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosVariacoes_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosAtributos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    AtributoVariacaoId = table.Column<long>(type: "bigint", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosAtributos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosAtributos_AtributosVariacao_AtributoVariacaoId",
                        column: x => x.AtributoVariacaoId,
                        principalTable: "AtributosVariacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProdutosAtributos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValoresAtributo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AtributoVariacaoId = table.Column<long>(type: "bigint", nullable: false),
                    Valor = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Hex = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValoresAtributo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValoresAtributo_AtributosVariacao_AtributoVariacaoId",
                        column: x => x.AtributoVariacaoId,
                        principalTable: "AtributosVariacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosVariacoesValores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoVariacaoId = table.Column<long>(type: "bigint", nullable: false),
                    AtributoVariacaoId = table.Column<long>(type: "bigint", nullable: false),
                    ValorAtributoId = table.Column<long>(type: "bigint", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosVariacoesValores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosVariacoesValores_AtributosVariacao_AtributoVariacao~",
                        column: x => x.AtributoVariacaoId,
                        principalTable: "AtributosVariacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProdutosVariacoesValores_ProdutosVariacoes_ProdutoVariacaoId",
                        column: x => x.ProdutoVariacaoId,
                        principalTable: "ProdutosVariacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutosVariacoesValores_ValoresAtributo_ValorAtributoId",
                        column: x => x.ValorAtributoId,
                        principalTable: "ValoresAtributo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendaItens_ProdutoVariacaoId",
                table: "VendaItens",
                column: "ProdutoVariacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_ProdutoVariacaoId",
                table: "ProdutosDados",
                column: "ProdutoVariacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_AtributosVariacao_Codigo",
                table: "AtributosVariacao",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AtributosVariacao_SyncGuid",
                table: "AtributosVariacao",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosAtributos_AtributoVariacaoId",
                table: "ProdutosAtributos",
                column: "AtributoVariacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosAtributos_Codigo",
                table: "ProdutosAtributos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosAtributos_ProdutoId_AtributoVariacaoId",
                table: "ProdutosAtributos",
                columns: new[] { "ProdutoId", "AtributoVariacaoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosAtributos_SyncGuid",
                table: "ProdutosAtributos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoes_Codigo",
                table: "ProdutosVariacoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoes_CodigoBarras",
                table: "ProdutosVariacoes",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoes_ProdutoId",
                table: "ProdutosVariacoes",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoes_SyncGuid",
                table: "ProdutosVariacoes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoesValores_AtributoVariacaoId",
                table: "ProdutosVariacoesValores",
                column: "AtributoVariacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoesValores_Codigo",
                table: "ProdutosVariacoesValores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoesValores_ProdutoVariacaoId",
                table: "ProdutosVariacoesValores",
                column: "ProdutoVariacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoesValores_SyncGuid",
                table: "ProdutosVariacoesValores",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoesValores_ValorAtributoId",
                table: "ProdutosVariacoesValores",
                column: "ValorAtributoId");

            migrationBuilder.CreateIndex(
                name: "IX_ValoresAtributo_AtributoVariacaoId",
                table: "ValoresAtributo",
                column: "AtributoVariacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ValoresAtributo_Codigo",
                table: "ValoresAtributo",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ValoresAtributo_SyncGuid",
                table: "ValoresAtributo",
                column: "SyncGuid");

            migrationBuilder.AddForeignKey(
                name: "FK_ProdutosDados_ProdutosVariacoes_ProdutoVariacaoId",
                table: "ProdutosDados",
                column: "ProdutoVariacaoId",
                principalTable: "ProdutosVariacoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VendaItens_ProdutosVariacoes_ProdutoVariacaoId",
                table: "VendaItens",
                column: "ProdutoVariacaoId",
                principalTable: "ProdutosVariacoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProdutosDados_ProdutosVariacoes_ProdutoVariacaoId",
                table: "ProdutosDados");

            migrationBuilder.DropForeignKey(
                name: "FK_VendaItens_ProdutosVariacoes_ProdutoVariacaoId",
                table: "VendaItens");

            migrationBuilder.DropTable(
                name: "ProdutosAtributos");

            migrationBuilder.DropTable(
                name: "ProdutosVariacoesValores");

            migrationBuilder.DropTable(
                name: "ProdutosVariacoes");

            migrationBuilder.DropTable(
                name: "ValoresAtributo");

            migrationBuilder.DropTable(
                name: "AtributosVariacao");

            migrationBuilder.DropIndex(
                name: "IX_VendaItens_ProdutoVariacaoId",
                table: "VendaItens");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosDados_ProdutoVariacaoId",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "ProdutoVariacaoId",
                table: "VendaItens");

            migrationBuilder.DropColumn(
                name: "ProdutoVariacaoId",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "ControlaGrade",
                table: "Produtos");
        }
    }
}
