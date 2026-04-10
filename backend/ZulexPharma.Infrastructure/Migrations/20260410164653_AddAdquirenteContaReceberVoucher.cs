using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdquirenteContaReceberVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adquirentes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adquirentes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    VendaOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorUtilizado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataValidade = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    table.PrimaryKey("PK_Vouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vouchers_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vouchers_Vendas_VendaOrigemId",
                        column: x => x.VendaOrigemId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AdquirenteBandeiras",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdquirenteId = table.Column<long>(type: "bigint", nullable: false),
                    Bandeira = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdquirenteBandeiras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdquirenteBandeiras_Adquirentes_AdquirenteId",
                        column: x => x.AdquirenteId,
                        principalTable: "Adquirentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdquirenteTarifas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdquirenteBandeiraId = table.Column<long>(type: "bigint", nullable: false),
                    Modalidade = table.Column<int>(type: "integer", nullable: false),
                    Tarifa = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    PrazoRecebimento = table.Column<int>(type: "integer", nullable: false),
                    ContaBancariaId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdquirenteTarifas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdquirenteTarifas_AdquirenteBandeiras_AdquirenteBandeiraId",
                        column: x => x.AdquirenteBandeiraId,
                        principalTable: "AdquirenteBandeiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdquirenteTarifas_ContasBancarias_ContaBancariaId",
                        column: x => x.ContaBancariaId,
                        principalTable: "ContasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ContasReceber",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    VendaId = table.Column<long>(type: "bigint", nullable: true),
                    VendaPagamentoId = table.Column<long>(type: "bigint", nullable: true),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true),
                    PessoaId = table.Column<long>(type: "bigint", nullable: true),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: true),
                    PlanoContaId = table.Column<long>(type: "bigint", nullable: true),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorLiquido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Tarifa = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    ValorTarifa = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorRecebido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorJuros = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataRecebimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NumParcela = table.Column<int>(type: "integer", nullable: false),
                    TotalParcelas = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContaBancariaId = table.Column<long>(type: "bigint", nullable: true),
                    AdquirenteBandeiraId = table.Column<long>(type: "bigint", nullable: true),
                    AdquirenteTarifaId = table.Column<long>(type: "bigint", nullable: true),
                    Modalidade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NSU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TxId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_ContasReceber", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasReceber_AdquirenteBandeiras_AdquirenteBandeiraId",
                        column: x => x.AdquirenteBandeiraId,
                        principalTable: "AdquirenteBandeiras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasReceber_AdquirenteTarifas_AdquirenteTarifaId",
                        column: x => x.AdquirenteTarifaId,
                        principalTable: "AdquirenteTarifas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasReceber_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasReceber_ContasBancarias_ContaBancariaId",
                        column: x => x.ContaBancariaId,
                        principalTable: "ContasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasReceber_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContasReceber_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasReceber_PlanosContas_PlanoContaId",
                        column: x => x.PlanoContaId,
                        principalTable: "PlanosContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasReceber_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasReceber_VendaPagamentos_VendaPagamentoId",
                        column: x => x.VendaPagamentoId,
                        principalTable: "VendaPagamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasReceber_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasReceber_Vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "Vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdquirenteBandeiras_AdquirenteId",
                table: "AdquirenteBandeiras",
                column: "AdquirenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Adquirentes_Codigo",
                table: "Adquirentes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Adquirentes_SyncGuid",
                table: "Adquirentes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_AdquirenteTarifas_AdquirenteBandeiraId",
                table: "AdquirenteTarifas",
                column: "AdquirenteBandeiraId");

            migrationBuilder.CreateIndex(
                name: "IX_AdquirenteTarifas_ContaBancariaId",
                table: "AdquirenteTarifas",
                column: "ContaBancariaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_AdquirenteBandeiraId",
                table: "ContasReceber",
                column: "AdquirenteBandeiraId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_AdquirenteTarifaId",
                table: "ContasReceber",
                column: "AdquirenteTarifaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_ClienteId",
                table: "ContasReceber",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_Codigo",
                table: "ContasReceber",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_ContaBancariaId",
                table: "ContasReceber",
                column: "ContaBancariaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_DataVencimento",
                table: "ContasReceber",
                column: "DataVencimento");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_FilialId",
                table: "ContasReceber",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_PessoaId",
                table: "ContasReceber",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_PlanoContaId",
                table: "ContasReceber",
                column: "PlanoContaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_Status",
                table: "ContasReceber",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_SyncGuid",
                table: "ContasReceber",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_TipoPagamentoId",
                table: "ContasReceber",
                column: "TipoPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_VendaId",
                table: "ContasReceber",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_VendaPagamentoId",
                table: "ContasReceber",
                column: "VendaPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_VoucherId",
                table: "ContasReceber",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_ClienteId",
                table: "Vouchers",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Codigo",
                table: "Vouchers",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_SyncGuid",
                table: "Vouchers",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_VendaOrigemId",
                table: "Vouchers",
                column: "VendaOrigemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContasReceber");

            migrationBuilder.DropTable(
                name: "AdquirenteTarifas");

            migrationBuilder.DropTable(
                name: "Vouchers");

            migrationBuilder.DropTable(
                name: "AdquirenteBandeiras");

            migrationBuilder.DropTable(
                name: "Adquirentes");
        }
    }
}
