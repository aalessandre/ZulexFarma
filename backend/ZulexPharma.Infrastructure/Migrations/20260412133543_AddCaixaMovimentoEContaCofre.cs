using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaixaMovimentoEContaCofre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ContaCofreId",
                table: "Filiais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeloFechamento",
                table: "Caixas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaixaFechamentoDeclarados",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaixaId = table.Column<long>(type: "bigint", nullable: false),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: false),
                    ValorDeclarado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaixaFechamentoDeclarados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaixaFechamentoDeclarados_Caixas_CaixaId",
                        column: x => x.CaixaId,
                        principalTable: "Caixas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaixaFechamentoDeclarados_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaixaMovimentos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CaixaId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    DataMovimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: true),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VendaPagamentoId = table.Column<long>(type: "bigint", nullable: true),
                    ContaReceberId = table.Column<long>(type: "bigint", nullable: true),
                    ContaPagarId = table.Column<long>(type: "bigint", nullable: true),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    StatusConferencia = table.Column<int>(type: "integer", nullable: false),
                    DataConferencia = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ConferidoPorUsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    ConferenteUsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    DataConferenteSangria = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaixaMovimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaixaMovimentos_Caixas_CaixaId",
                        column: x => x.CaixaId,
                        principalTable: "Caixas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaixaMovimentos_ContasPagar_ContaPagarId",
                        column: x => x.ContaPagarId,
                        principalTable: "ContasPagar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CaixaMovimentos_ContasReceber_ContaReceberId",
                        column: x => x.ContaReceberId,
                        principalTable: "ContasReceber",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CaixaMovimentos_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CaixaMovimentos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CaixaMovimentos_VendaPagamentos_VendaPagamentoId",
                        column: x => x.VendaPagamentoId,
                        principalTable: "VendaPagamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MovimentosContaBancaria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContaBancariaId = table.Column<long>(type: "bigint", nullable: false),
                    DataMovimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CaixaMovimentoId = table.Column<long>(type: "bigint", nullable: true),
                    CaixaId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_MovimentosContaBancaria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosContaBancaria_CaixaMovimentos_CaixaMovimentoId",
                        column: x => x.CaixaMovimentoId,
                        principalTable: "CaixaMovimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimentosContaBancaria_Caixas_CaixaId",
                        column: x => x.CaixaId,
                        principalTable: "Caixas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimentosContaBancaria_ContasBancarias_ContaBancariaId",
                        column: x => x.ContaBancariaId,
                        principalTable: "ContasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosContaBancaria_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Filiais_ContaCofreId",
                table: "Filiais",
                column: "ContaCofreId");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaFechamentoDeclarados_CaixaId_TipoPagamentoId",
                table: "CaixaFechamentoDeclarados",
                columns: new[] { "CaixaId", "TipoPagamentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaixaFechamentoDeclarados_Codigo",
                table: "CaixaFechamentoDeclarados",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaFechamentoDeclarados_SyncGuid",
                table: "CaixaFechamentoDeclarados",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaFechamentoDeclarados_TipoPagamentoId",
                table: "CaixaFechamentoDeclarados",
                column: "TipoPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_CaixaId",
                table: "CaixaMovimentos",
                column: "CaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_Codigo",
                table: "CaixaMovimentos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_ContaPagarId",
                table: "CaixaMovimentos",
                column: "ContaPagarId");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_ContaReceberId",
                table: "CaixaMovimentos",
                column: "ContaReceberId");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_StatusConferencia",
                table: "CaixaMovimentos",
                column: "StatusConferencia");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_SyncGuid",
                table: "CaixaMovimentos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_Tipo",
                table: "CaixaMovimentos",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_TipoPagamentoId",
                table: "CaixaMovimentos",
                column: "TipoPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_UsuarioId",
                table: "CaixaMovimentos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_VendaPagamentoId",
                table: "CaixaMovimentos",
                column: "VendaPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_CaixaId",
                table: "MovimentosContaBancaria",
                column: "CaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_CaixaMovimentoId",
                table: "MovimentosContaBancaria",
                column: "CaixaMovimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_Codigo",
                table: "MovimentosContaBancaria",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_ContaBancariaId_DataMovimento",
                table: "MovimentosContaBancaria",
                columns: new[] { "ContaBancariaId", "DataMovimento" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_SyncGuid",
                table: "MovimentosContaBancaria",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_UsuarioId",
                table: "MovimentosContaBancaria",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Filiais_ContasBancarias_ContaCofreId",
                table: "Filiais",
                column: "ContaCofreId",
                principalTable: "ContasBancarias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Filiais_ContasBancarias_ContaCofreId",
                table: "Filiais");

            migrationBuilder.DropTable(
                name: "CaixaFechamentoDeclarados");

            migrationBuilder.DropTable(
                name: "MovimentosContaBancaria");

            migrationBuilder.DropTable(
                name: "CaixaMovimentos");

            migrationBuilder.DropIndex(
                name: "IX_Filiais_ContaCofreId",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "ContaCofreId",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "ModeloFechamento",
                table: "Caixas");
        }
    }
}
