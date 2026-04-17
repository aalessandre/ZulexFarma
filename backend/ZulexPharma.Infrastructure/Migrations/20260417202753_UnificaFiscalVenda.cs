using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnificaFiscalVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nfces");

            migrationBuilder.DropTable(
                name: "NfeItens");

            migrationBuilder.DropTable(
                name: "NfeParcelas");

            migrationBuilder.DropTable(
                name: "Perdas");

            migrationBuilder.DropTable(
                name: "Nfes");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId",
                table: "Vendas");

            migrationBuilder.AddColumn<long>(
                name: "DestinatarioPessoaId",
                table: "Vendas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FilialDestinoId",
                table: "Vendas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModeloDocumento",
                table: "Vendas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Motivo",
                table: "Vendas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NaturezaOperacaoId",
                table: "Vendas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroBoletim",
                table: "Vendas",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusFiscal",
                table: "Vendas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoOperacao",
                table: "Vendas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VendaFiscais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaId = table.Column<long>(type: "bigint", nullable: false),
                    Modelo = table.Column<int>(type: "integer", nullable: false),
                    Finalidade = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    Protocolo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAutorizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataSaidaEntrada = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ambiente = table.Column<int>(type: "integer", nullable: false),
                    TipoNf = table.Column<int>(type: "integer", nullable: false),
                    IdentificadorDestino = table.Column<int>(type: "integer", nullable: false),
                    CodigoStatus = table.Column<int>(type: "integer", nullable: false),
                    MotivoStatus = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NatOp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NaturezaOperacaoId = table.Column<long>(type: "bigint", nullable: true),
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
                    XmlEnvio = table.Column<string>(type: "text", nullable: true),
                    XmlRetorno = table.Column<string>(type: "text", nullable: true),
                    XmlCancelamento = table.Column<string>(type: "text", nullable: true),
                    XmlCartaCorrecao = table.Column<string>(type: "text", nullable: true),
                    ChaveNfeReferenciada = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaFiscais_NaturezasOperacao_NaturezaOperacaoId",
                        column: x => x.NaturezaOperacaoId,
                        principalTable: "NaturezasOperacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendaFiscais_Pessoas_TransportadoraPessoaId",
                        column: x => x.TransportadoraPessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendaFiscais_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendaItemFiscais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaItemId = table.Column<long>(type: "bigint", nullable: false),
                    NumeroItem = table.Column<int>(type: "integer", nullable: false),
                    CodigoProduto = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescricaoProduto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Ncm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Cest = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Cfop = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Unidade = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
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
                    CustoUnitario = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaItemFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaItemFiscais_VendaItens_VendaItemId",
                        column: x => x.VendaItemId,
                        principalTable: "VendaItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_DestinatarioPessoaId",
                table: "Vendas",
                column: "DestinatarioPessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialDestinoId",
                table: "Vendas",
                column: "FilialDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId_StatusFiscal",
                table: "Vendas",
                columns: new[] { "FilialId", "StatusFiscal" });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId_TipoOperacao",
                table: "Vendas",
                columns: new[] { "FilialId", "TipoOperacao" });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_NaturezaOperacaoId",
                table: "Vendas",
                column: "NaturezaOperacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_ChaveAcesso",
                table: "VendaFiscais",
                column: "ChaveAcesso",
                unique: true,
                filter: "\"ChaveAcesso\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_Codigo",
                table: "VendaFiscais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_NaturezaOperacaoId",
                table: "VendaFiscais",
                column: "NaturezaOperacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_SyncGuid",
                table: "VendaFiscais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_TransportadoraPessoaId",
                table: "VendaFiscais",
                column: "TransportadoraPessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_VendaId",
                table: "VendaFiscais",
                column: "VendaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaItemFiscais_Codigo",
                table: "VendaItemFiscais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItemFiscais_SyncGuid",
                table: "VendaItemFiscais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItemFiscais_VendaItemId",
                table: "VendaItemFiscais",
                column: "VendaItemId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_Filiais_FilialDestinoId",
                table: "Vendas",
                column: "FilialDestinoId",
                principalTable: "Filiais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_NaturezasOperacao_NaturezaOperacaoId",
                table: "Vendas",
                column: "NaturezaOperacaoId",
                principalTable: "NaturezasOperacao",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_Pessoas_DestinatarioPessoaId",
                table: "Vendas",
                column: "DestinatarioPessoaId",
                principalTable: "Pessoas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_Filiais_FilialDestinoId",
                table: "Vendas");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_NaturezasOperacao_NaturezaOperacaoId",
                table: "Vendas");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_Pessoas_DestinatarioPessoaId",
                table: "Vendas");

            migrationBuilder.DropTable(
                name: "VendaFiscais");

            migrationBuilder.DropTable(
                name: "VendaItemFiscais");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_DestinatarioPessoaId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialDestinoId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId_StatusFiscal",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId_TipoOperacao",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_NaturezaOperacaoId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "DestinatarioPessoaId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "FilialDestinoId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "ModeloDocumento",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "Motivo",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "NaturezaOperacaoId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "NumeroBoletim",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "StatusFiscal",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "TipoOperacao",
                table: "Vendas");

            migrationBuilder.CreateTable(
                name: "Nfces",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    VendaId = table.Column<long>(type: "bigint", nullable: false),
                    Ambiente = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CodigoStatus = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAutorizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    MotivoStatus = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Protocolo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    XmlEnvio = table.Column<string>(type: "text", nullable: true),
                    XmlRetorno = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nfces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nfces_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Nfces_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Nfes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DestinatarioPessoaId = table.Column<long>(type: "bigint", nullable: true),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    NaturezaOperacaoId = table.Column<long>(type: "bigint", nullable: false),
                    TransportadoraPessoaId = table.Column<long>(type: "bigint", nullable: true),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    Ambiente = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    ChaveNfeReferenciada = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CodigoStatus = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataAutorizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataSaidaEntrada = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    FinalidadeNfe = table.Column<int>(type: "integer", nullable: false),
                    IdentificadorDestino = table.Column<int>(type: "integer", nullable: false),
                    ModFrete = table.Column<int>(type: "integer", nullable: false),
                    MotivoStatus = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NatOp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    NumeroFatura = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PlacaVeiculo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Protocolo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TipoNf = table.Column<int>(type: "integer", nullable: false),
                    UfVeiculo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ValorCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorFrete = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIcmsSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIpi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorLiquidoFatura = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ValorNota = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorOriginalFatura = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ValorOutros = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorProdutos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorSeguro = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotalTributos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VolumeEspecie = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VolumePesoBruto = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    VolumePesoLiquido = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    VolumeQuantidade = table.Column<int>(type: "integer", nullable: true),
                    XmlCancelamento = table.Column<string>(type: "text", nullable: true),
                    XmlCartaCorrecao = table.Column<string>(type: "text", nullable: true),
                    XmlEnvio = table.Column<string>(type: "text", nullable: true),
                    XmlRetorno = table.Column<string>(type: "text", nullable: true)
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
                name: "Perdas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoLoteId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataPerda = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    Motivo = table.Column<int>(type: "integer", nullable: false),
                    NumeroBoletim = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perdas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perdas_ProdutosLotes_ProdutoLoteId",
                        column: x => x.ProdutoLoteId,
                        principalTable: "ProdutosLotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Perdas_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Perdas_Usuarios_UsuarioId",
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
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    AliquotaCofins = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AliquotaFcp = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AliquotaFcpSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AliquotaIcms = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AliquotaIcmsSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AliquotaIpi = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AliquotaPis = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BaseCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseFcp = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseFcpSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseIcmsSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseIpi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BasePis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Cest = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Cfop = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CodigoAnvisa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CodigoBarras = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CodigoBeneficioFiscal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CodigoProduto = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Csosn = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CstCofins = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CstIcms = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CstIpi = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    CstPis = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    DescricaoProduto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    EnquadramentoIpi = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    IndicadorTotal = table.Column<int>(type: "integer", nullable: false),
                    ModBcIcms = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ModBcIcmsSt = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    MotivoDesoneracaoIcms = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    MvaSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Ncm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NumeroItem = table.Column<int>(type: "integer", nullable: false),
                    OrigemMercadoria = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    PercentualReducaoBc = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProdutoLoteId = table.Column<long>(type: "bigint", nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RastroFabricacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RastroLote = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RastroQuantidade = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    RastroValidade = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Unidade = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorFcp = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorFcpSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorFrete = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIcmsDesonerado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIcmsSt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIpi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorOutros = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorSeguro = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotalTributos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
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
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    NumeroParcela = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
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
                name: "IX_Vendas_FilialId",
                table: "Vendas",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_ChaveAcesso",
                table: "Nfces",
                column: "ChaveAcesso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_Codigo",
                table: "Nfces",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_FilialId_Serie_Numero",
                table: "Nfces",
                columns: new[] { "FilialId", "Serie", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_SyncGuid",
                table: "Nfces",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_VendaId",
                table: "Nfces",
                column: "VendaId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Perdas_Codigo",
                table: "Perdas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Perdas_FilialId_DataPerda",
                table: "Perdas",
                columns: new[] { "FilialId", "DataPerda" });

            migrationBuilder.CreateIndex(
                name: "IX_Perdas_ProdutoId",
                table: "Perdas",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Perdas_ProdutoLoteId",
                table: "Perdas",
                column: "ProdutoLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Perdas_SyncGuid",
                table: "Perdas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Perdas_UsuarioId",
                table: "Perdas",
                column: "UsuarioId");
        }
    }
}
