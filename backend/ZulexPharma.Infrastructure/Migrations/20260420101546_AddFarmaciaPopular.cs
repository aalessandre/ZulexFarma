using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmaciaPopular : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ParticipaFarmaciaPopular",
                table: "Produtos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoFpBolsaFamilia",
                table: "Produtos",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VendaFarmaciaPopulares",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaId = table.Column<long>(type: "bigint", nullable: false),
                    CoSolicitacaoFarmacia = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    NuAutorizacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NuCupomFiscal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DnaEstacao = table.Column<string>(type: "text", nullable: true),
                    CnpjEstabelecimento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    CpfPaciente = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    NoPaciente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BolsaFamilia = table.Column<bool>(type: "boolean", nullable: false),
                    CrmMedico = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UfCrm = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DtEmissaoReceita = table.Column<DateOnly>(type: "date", nullable: false),
                    NuReceita = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PrescritorId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FaseAtual = table.Column<int>(type: "integer", nullable: false),
                    CodigoRetornoAtual = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    MensagemRetornoAtual = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EstornoPendente = table.Column<bool>(type: "boolean", nullable: false),
                    Fase1RequestXml = table.Column<string>(type: "text", nullable: true),
                    Fase1ResponseXml = table.Column<string>(type: "text", nullable: true),
                    Fase1DataHora = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Fase2RequestXml = table.Column<string>(type: "text", nullable: true),
                    Fase2ResponseXml = table.Column<string>(type: "text", nullable: true),
                    Fase2DataHora = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Fase3RequestXml = table.Column<string>(type: "text", nullable: true),
                    Fase3ResponseXml = table.Column<string>(type: "text", nullable: true),
                    Fase3DataHora = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EstornoRequestXml = table.Column<string>(type: "text", nullable: true),
                    EstornoResponseXml = table.Column<string>(type: "text", nullable: true),
                    EstornoDataHora = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaFarmaciaPopulares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaFarmaciaPopulares_Prescritores_PrescritorId",
                        column: x => x.PrescritorId,
                        principalTable: "Prescritores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendaFarmaciaPopulares_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendaFarmaciaPopularItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaFarmaciaPopularId = table.Column<long>(type: "bigint", nullable: false),
                    VendaItemId = table.Column<long>(type: "bigint", nullable: false),
                    CodigoBarraEAN = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    QtPrescrita = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    QtSolicitada = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    QtAutorizada = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    QtDispensada = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    QtEstornada = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    VlPrecoVenda = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    VlPrecoSubsidiadoMS = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    VlPrecoSubsidiadoPaciente = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    CodigoRetornoItem = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    MensagemRetornoItem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InAutorizacaoMedicamento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaFarmaciaPopularItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaFarmaciaPopularItens_VendaFarmaciaPopulares_VendaFarma~",
                        column: x => x.VendaFarmaciaPopularId,
                        principalTable: "VendaFarmaciaPopulares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendaFarmaciaPopularItens_VendaItens_VendaItemId",
                        column: x => x.VendaItemId,
                        principalTable: "VendaItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_Codigo",
                table: "VendaFarmaciaPopulares",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_CoSolicitacaoFarmacia",
                table: "VendaFarmaciaPopulares",
                column: "CoSolicitacaoFarmacia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_CpfPaciente",
                table: "VendaFarmaciaPopulares",
                column: "CpfPaciente");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_NuAutorizacao",
                table: "VendaFarmaciaPopulares",
                column: "NuAutorizacao");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_PrescritorId",
                table: "VendaFarmaciaPopulares",
                column: "PrescritorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_SyncGuid",
                table: "VendaFarmaciaPopulares",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_VendaId",
                table: "VendaFarmaciaPopulares",
                column: "VendaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopularItens_Codigo",
                table: "VendaFarmaciaPopularItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopularItens_SyncGuid",
                table: "VendaFarmaciaPopularItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopularItens_VendaFarmaciaPopularId",
                table: "VendaFarmaciaPopularItens",
                column: "VendaFarmaciaPopularId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopularItens_VendaItemId",
                table: "VendaFarmaciaPopularItens",
                column: "VendaItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendaFarmaciaPopularItens");

            migrationBuilder.DropTable(
                name: "VendaFarmaciaPopulares");

            migrationBuilder.DropColumn(
                name: "ParticipaFarmaciaPopular",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "PrecoFpBolsaFamilia",
                table: "Produtos");
        }
    }
}
