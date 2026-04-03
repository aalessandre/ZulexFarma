using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComprasModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataValidade",
                table: "ProdutosDados",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lote",
                table: "ProdutosDados",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    FornecedorId = table.Column<long>(type: "bigint", nullable: false),
                    ChaveNfe = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    NumeroNf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SerieNf = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    NaturezaOperacao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataEntrada = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValorProdutos = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorSt = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorFcpSt = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorFrete = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorSeguro = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorIpi = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorOutros = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorNota = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    XmlConteudo = table.Column<string>(type: "text", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compras_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComprasProdutos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompraId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    NumeroItem = table.Column<int>(type: "integer", nullable: false),
                    CodigoProdutoFornecedor = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CodigoBarrasXml = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    DescricaoXml = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    NcmXml = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CestXml = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    CfopXml = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    UnidadeXml = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(12,6)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorFrete = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorOutros = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ValorItemNota = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Lote = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DataFabricacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataValidade = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CodigoAnvisa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PrecoMaximoConsumidor = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Vinculado = table.Column<bool>(type: "boolean", nullable: false),
                    InfoAdicional = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprasProdutos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprasProdutos_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComprasProdutos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComprasFiscal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompraProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    OrigemMercadoria = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    CstIcms = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    BaseIcms = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AliquotaIcms = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorIcms = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ModalidadeBcSt = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    MvaSt = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    BaseSt = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AliquotaSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorSt = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    BaseFcpSt = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AliquotaFcpSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorFcpSt = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CstPis = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    BasePis = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AliquotaPis = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CstCofins = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    BaseCofins = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AliquotaCofins = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CstIbsCbs = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ClasseTributariaIbsCbs = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BaseIbsCbs = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AliquotaIbsUf = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorIbsUf = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AliquotaIbsMun = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorIbsMun = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AliquotaCbs = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorCbs = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprasFiscal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprasFiscal_ComprasProdutos_CompraProdutoId",
                        column: x => x.CompraProdutoId,
                        principalTable: "ComprasProdutos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Compras_ChaveNfe",
                table: "Compras",
                column: "ChaveNfe",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Codigo",
                table: "Compras",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_FilialId",
                table: "Compras",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_FornecedorId",
                table: "Compras",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_SyncGuid",
                table: "Compras",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasFiscal_Codigo",
                table: "ComprasFiscal",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasFiscal_CompraProdutoId",
                table: "ComprasFiscal",
                column: "CompraProdutoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprasFiscal_SyncGuid",
                table: "ComprasFiscal",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutos_Codigo",
                table: "ComprasProdutos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutos_CompraId_NumeroItem",
                table: "ComprasProdutos",
                columns: new[] { "CompraId", "NumeroItem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutos_ProdutoId",
                table: "ComprasProdutos",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutos_SyncGuid",
                table: "ComprasProdutos",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComprasFiscal");

            migrationBuilder.DropTable(
                name: "ComprasProdutos");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropColumn(
                name: "DataValidade",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "Lote",
                table: "ProdutosDados");
        }
    }
}
