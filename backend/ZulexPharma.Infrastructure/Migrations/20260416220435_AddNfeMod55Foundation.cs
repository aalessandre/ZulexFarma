using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNfeMod55Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoIbgeMunicipio",
                table: "PessoasEndereco",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NaturezasOperacao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoNf = table.Column<int>(type: "integer", nullable: false),
                    FinalidadeNfe = table.Column<int>(type: "integer", nullable: false),
                    IdentificadorDestino = table.Column<int>(type: "integer", nullable: false),
                    RelacionarDocumentoFiscal = table.Column<bool>(type: "boolean", nullable: false),
                    UtilizarPrecoCusto = table.Column<bool>(type: "boolean", nullable: false),
                    ReajustarCustoMedio = table.Column<bool>(type: "boolean", nullable: false),
                    GeraFinanceiro = table.Column<bool>(type: "boolean", nullable: false),
                    MovimentaEstoque = table.Column<bool>(type: "boolean", nullable: false),
                    TipoMovimentoEstoque = table.Column<int>(type: "integer", nullable: true),
                    CstPisPadrao = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CstCofinsPadrao = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CstIpiPadrao = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    EnquadramentoIpiPadrao = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IndicadorPresenca = table.Column<int>(type: "integer", nullable: false),
                    IndicadorFinalidade = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_NaturezasOperacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NaturezaOperacaoRegras",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NaturezaOperacaoId = table.Column<long>(type: "bigint", nullable: false),
                    CenarioTributario = table.Column<int>(type: "integer", nullable: false),
                    CfopInterno = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CfopInterestadual = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CstIcmsInterno = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CstIcmsInterestadual = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CodigoBeneficioInterno = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    CodigoBeneficioInterestadual = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NaturezaOperacaoRegras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NaturezaOperacaoRegras_NaturezasOperacao_NaturezaOperacaoId",
                        column: x => x.NaturezaOperacaoId,
                        principalTable: "NaturezasOperacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nfes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    NaturezaOperacaoId = table.Column<long>(type: "bigint", nullable: false),
                    DestinatarioPessoaId = table.Column<long>(type: "bigint", nullable: true),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    Protocolo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAutorizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataSaidaEntrada = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ambiente = table.Column<int>(type: "integer", nullable: false),
                    TipoNf = table.Column<int>(type: "integer", nullable: false),
                    FinalidadeNfe = table.Column<int>(type: "integer", nullable: false),
                    IdentificadorDestino = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CodigoStatus = table.Column<int>(type: "integer", nullable: false),
                    MotivoStatus = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NatOp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ModFrete = table.Column<int>(type: "integer", nullable: false),
                    TransportadoraPessoaId = table.Column<long>(type: "bigint", nullable: true),
                    PlacaVeiculo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    UfVeiculo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    VolumeQuantidade = table.Column<int>(type: "integer", nullable: true),
                    VolumeEspecie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VolumePesoLiquido = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    VolumePesoBruto = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    ValorProdutos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorFrete = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorSeguro = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorOutros = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIcmsSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIpi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorNota = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotalTributos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NumeroFatura = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ValorOriginalFatura = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ValorLiquidoFatura = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    XmlEnvio = table.Column<string>(type: "text", nullable: true),
                    XmlRetorno = table.Column<string>(type: "text", nullable: true),
                    XmlCancelamento = table.Column<string>(type: "text", nullable: true),
                    XmlCartaCorrecao = table.Column<string>(type: "text", nullable: true),
                    ChaveNfeReferenciada = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nfes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nfes_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Nfes_NaturezasOperacao_NaturezaOperacaoId",
                        column: x => x.NaturezaOperacaoId,
                        principalTable: "NaturezasOperacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Nfes_Pessoas_DestinatarioPessoaId",
                        column: x => x.DestinatarioPessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Nfes_Pessoas_TransportadoraPessoaId",
                        column: x => x.TransportadoraPessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Nfes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NfeItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NfeId = table.Column<long>(type: "bigint", nullable: false),
                    NumeroItem = table.Column<int>(type: "integer", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoLoteId = table.Column<long>(type: "bigint", nullable: true),
                    CodigoProduto = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescricaoProduto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Ncm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Cest = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Cfop = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Unidade = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorFrete = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorSeguro = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorOutros = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IndicadorTotal = table.Column<int>(type: "integer", nullable: false),
                    CodigoAnvisa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RastroLote = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RastroFabricacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RastroValidade = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RastroQuantidade = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    OrigemMercadoria = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CstIcms = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Csosn = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    ModBcIcms = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    BaseIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AliquotaIcms = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PercentualReducaoBc = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorIcmsDesonerado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MotivoDesoneracaoIcms = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CodigoBeneficioFiscal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ModBcIcmsSt = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    MvaSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    BaseIcmsSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AliquotaIcmsSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorIcmsSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseFcp = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AliquotaFcp = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorFcp = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseFcpSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AliquotaFcpSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorFcpSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CstPis = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    BasePis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AliquotaPis = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CstCofins = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    BaseCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AliquotaCofins = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CstIpi = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    EnquadramentoIpi = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BaseIpi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AliquotaIpi = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorIpi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotalTributos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NfeItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NfeItens_Nfes_NfeId",
                        column: x => x.NfeId,
                        principalTable: "Nfes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NfeItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NfeParcelas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NfeId = table.Column<long>(type: "bigint", nullable: false),
                    NumeroParcela = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NfeParcelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NfeParcelas_Nfes_NfeId",
                        column: x => x.NfeId,
                        principalTable: "Nfes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NaturezaOperacaoRegras_Codigo",
                table: "NaturezaOperacaoRegras",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NaturezaOperacaoRegras_NaturezaOperacaoId",
                table: "NaturezaOperacaoRegras",
                column: "NaturezaOperacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_NaturezaOperacaoRegras_SyncGuid",
                table: "NaturezaOperacaoRegras",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_NaturezasOperacao_Codigo",
                table: "NaturezasOperacao",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NaturezasOperacao_SyncGuid",
                table: "NaturezasOperacao",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_NfeItens_Codigo",
                table: "NfeItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NfeItens_NfeId",
                table: "NfeItens",
                column: "NfeId");

            migrationBuilder.CreateIndex(
                name: "IX_NfeItens_ProdutoId",
                table: "NfeItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_NfeItens_SyncGuid",
                table: "NfeItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_NfeParcelas_Codigo",
                table: "NfeParcelas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NfeParcelas_NfeId",
                table: "NfeParcelas",
                column: "NfeId");

            migrationBuilder.CreateIndex(
                name: "IX_NfeParcelas_SyncGuid",
                table: "NfeParcelas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Nfes_ChaveAcesso",
                table: "Nfes",
                column: "ChaveAcesso",
                unique: true,
                filter: "\"ChaveAcesso\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Nfes_Codigo",
                table: "Nfes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Nfes_DestinatarioPessoaId",
                table: "Nfes",
                column: "DestinatarioPessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_Nfes_FilialId_Status",
                table: "Nfes",
                columns: new[] { "FilialId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Nfes_NaturezaOperacaoId",
                table: "Nfes",
                column: "NaturezaOperacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Nfes_Numero_Serie_FilialId",
                table: "Nfes",
                columns: new[] { "Numero", "Serie", "FilialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nfes_SyncGuid",
                table: "Nfes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Nfes_TransportadoraPessoaId",
                table: "Nfes",
                column: "TransportadoraPessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_Nfes_UsuarioId",
                table: "Nfes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NaturezaOperacaoRegras");

            migrationBuilder.DropTable(
                name: "NfeItens");

            migrationBuilder.DropTable(
                name: "NfeParcelas");

            migrationBuilder.DropTable(
                name: "Nfes");

            migrationBuilder.DropTable(
                name: "NaturezasOperacao");

            migrationBuilder.DropColumn(
                name: "CodigoIbgeMunicipio",
                table: "PessoasEndereco");
        }
    }
}
