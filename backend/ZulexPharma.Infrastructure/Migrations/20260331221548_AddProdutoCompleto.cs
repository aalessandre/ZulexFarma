using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    QtdeEmbalagem = table.Column<int>(type: "integer", nullable: false),
                    PrecoFp = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    Lista = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Indefinida"),
                    Fracao = table.Column<short>(type: "smallint", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false),
                    FabricanteId = table.Column<long>(type: "bigint", nullable: true),
                    GrupoPrincipalId = table.Column<long>(type: "bigint", nullable: true),
                    GrupoProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    SubGrupoId = table.Column<long>(type: "bigint", nullable: true),
                    NcmId = table.Column<long>(type: "bigint", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_Fabricantes_FabricanteId",
                        column: x => x.FabricanteId,
                        principalTable: "Fabricantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Produtos_GruposPrincipais_GrupoPrincipalId",
                        column: x => x.GrupoPrincipalId,
                        principalTable: "GruposPrincipais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Produtos_GruposProdutos_GrupoProdutoId",
                        column: x => x.GrupoProdutoId,
                        principalTable: "GruposProdutos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Produtos_Ncms_NcmId",
                        column: x => x.NcmId,
                        principalTable: "Ncms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Produtos_SubGrupos_SubGrupoId",
                        column: x => x.SubGrupoId,
                        principalTable: "SubGrupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosLocais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosLocais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosBarras",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    Barras = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosBarras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosBarras_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosDados",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    EstoqueAtual = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    EstoqueMinimo = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    EstoqueMaximo = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    Demanda = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    CurvaAbc = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    UltimaCompraUnitario = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    UltimaCompraSt = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    UltimaCompraOutros = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    UltimaCompraIpi = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    UltimaCompraFpc = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    UltimaCompraBoleto = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    UltimaCompraDifal = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    CustoMedio = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    ProjecaoLucro = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Markup = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorVenda = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    ValorPromocao = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    PromocaoInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PromocaoFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DescontoMinimo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaxSemSenha = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaxComSenha = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Comissao = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorIncentivo = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    ProdutoLocalId = table.Column<long>(type: "bigint", nullable: true),
                    SecaoId = table.Column<long>(type: "bigint", nullable: true),
                    ProdutoFamiliaId = table.Column<long>(type: "bigint", nullable: true),
                    NomeEtiqueta = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Mensagem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BloquearDesconto = table.Column<bool>(type: "boolean", nullable: false),
                    BloquearPromocao = table.Column<bool>(type: "boolean", nullable: false),
                    NaoAtualizarAbcfarma = table.Column<bool>(type: "boolean", nullable: false),
                    NaoAtualizarGestorTributario = table.Column<bool>(type: "boolean", nullable: false),
                    BloquearCompras = table.Column<bool>(type: "boolean", nullable: false),
                    ProdutoFormula = table.Column<bool>(type: "boolean", nullable: false),
                    BloquearComissao = table.Column<bool>(type: "boolean", nullable: false),
                    BloquearCoberturaOferta = table.Column<bool>(type: "boolean", nullable: false),
                    UsoContinuo = table.Column<bool>(type: "boolean", nullable: false),
                    AvisoFracao = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaCompraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaVendaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosDados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosDados_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosFiscal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    NcmId = table.Column<long>(type: "bigint", nullable: true),
                    Cest = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    OrigemMercadoria = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    CstIcms = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Csosn = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    AliquotaIcms = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CstPis = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    AliquotaPis = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CstCofins = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    AliquotaCofins = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CstIpi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    AliquotaIpi = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosFiscal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosFiscal_Ncms_NcmId",
                        column: x => x.NcmId,
                        principalTable: "Ncms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProdutosFiscal_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosFornecedores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    FornecedorId = table.Column<long>(type: "bigint", nullable: false),
                    CodigoProdutoFornecedor = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    NomeProduto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Fracao = table.Column<short>(type: "smallint", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosFornecedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosFornecedores_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutosFornecedores_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosMs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    NumeroMs = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosMs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosMs_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosSubstancias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    SubstanciaId = table.Column<long>(type: "bigint", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosSubstancias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosSubstancias_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutosSubstancias_Substancias_SubstanciaId",
                        column: x => x.SubstanciaId,
                        principalTable: "Substancias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Codigo",
                table: "Produtos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_FabricanteId",
                table: "Produtos",
                column: "FabricanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_GrupoPrincipalId",
                table: "Produtos",
                column: "GrupoPrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_GrupoProdutoId",
                table: "Produtos",
                column: "GrupoProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_NcmId",
                table: "Produtos",
                column: "NcmId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_SubGrupoId",
                table: "Produtos",
                column: "SubGrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosBarras_Codigo",
                table: "ProdutosBarras",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosBarras_ProdutoId",
                table: "ProdutosBarras",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_Codigo",
                table: "ProdutosDados",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_ProdutoId_FilialId",
                table: "ProdutosDados",
                columns: new[] { "ProdutoId", "FilialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFiscal_Codigo",
                table: "ProdutosFiscal",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFiscal_NcmId",
                table: "ProdutosFiscal",
                column: "NcmId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFiscal_ProdutoId",
                table: "ProdutosFiscal",
                column: "ProdutoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFornecedores_Codigo",
                table: "ProdutosFornecedores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFornecedores_FornecedorId",
                table: "ProdutosFornecedores",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFornecedores_ProdutoId",
                table: "ProdutosFornecedores",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLocais_Codigo",
                table: "ProdutosLocais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosMs_Codigo",
                table: "ProdutosMs",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosMs_ProdutoId",
                table: "ProdutosMs",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosSubstancias_Codigo",
                table: "ProdutosSubstancias",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosSubstancias_ProdutoId_SubstanciaId",
                table: "ProdutosSubstancias",
                columns: new[] { "ProdutoId", "SubstanciaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosSubstancias_SubstanciaId",
                table: "ProdutosSubstancias",
                column: "SubstanciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutosBarras");

            migrationBuilder.DropTable(
                name: "ProdutosDados");

            migrationBuilder.DropTable(
                name: "ProdutosFiscal");

            migrationBuilder.DropTable(
                name: "ProdutosFornecedores");

            migrationBuilder.DropTable(
                name: "ProdutosLocais");

            migrationBuilder.DropTable(
                name: "ProdutosMs");

            migrationBuilder.DropTable(
                name: "ProdutosSubstancias");

            migrationBuilder.DropTable(
                name: "Produtos");
        }
    }
}
