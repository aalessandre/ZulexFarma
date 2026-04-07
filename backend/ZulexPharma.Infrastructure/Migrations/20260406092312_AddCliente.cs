using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PessoaId = table.Column<long>(type: "bigint", nullable: false),
                    LimiteCredito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DescontoGeral = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PermiteFidelidade = table.Column<bool>(type: "boolean", nullable: false),
                    PrazoPagamento = table.Column<int>(type: "integer", nullable: false),
                    QtdeDias = table.Column<int>(type: "integer", nullable: true),
                    DiaFechamento = table.Column<int>(type: "integer", nullable: true),
                    DiaVencimento = table.Column<int>(type: "integer", nullable: true),
                    QtdeMeses = table.Column<int>(type: "integer", nullable: true),
                    PermiteVendaParcelada = table.Column<bool>(type: "boolean", nullable: false),
                    QtdeMaxParcelas = table.Column<int>(type: "integer", nullable: false),
                    PermiteVendaPrazo = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteVendaVista = table.Column<bool>(type: "boolean", nullable: false),
                    Bloqueado = table.Column<bool>(type: "boolean", nullable: false),
                    CalcularJuros = table.Column<bool>(type: "boolean", nullable: false),
                    BloquearComissao = table.Column<bool>(type: "boolean", nullable: false),
                    PedirSenhaVendaPrazo = table.Column<bool>(type: "boolean", nullable: false),
                    SenhaVendaPrazo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Aviso = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clientes_Pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClienteAutorizacoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteAutorizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClienteAutorizacoes_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClienteConvenios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    ConvenioId = table.Column<long>(type: "bigint", nullable: false),
                    Matricula = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Cartao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Limite = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteConvenios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClienteConvenios_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClienteConvenios_Convenios_ConvenioId",
                        column: x => x.ConvenioId,
                        principalTable: "Convenios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClienteDescontos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: true),
                    TipoAgrupador = table.Column<int>(type: "integer", nullable: true),
                    AgrupadorId = table.Column<long>(type: "bigint", nullable: true),
                    AgrupadorOuProdutoNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DescontoMaxSemSenha = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DescontoMaxComSenha = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteDescontos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClienteDescontos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClienteUsosContinuos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    Fabricante = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Apresentacao = table.Column<int>(type: "integer", nullable: false),
                    QtdeAoDia = table.Column<int>(type: "integer", nullable: false),
                    UltimaCompra = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProximaCompra = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ColaboradorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteUsosContinuos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClienteUsosContinuos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClienteUsosContinuos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClienteAutorizacoes_ClienteId",
                table: "ClienteAutorizacoes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteConvenios_ClienteId",
                table: "ClienteConvenios",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteConvenios_ConvenioId",
                table: "ClienteConvenios",
                column: "ConvenioId");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteDescontos_ClienteId",
                table: "ClienteDescontos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Codigo",
                table: "Clientes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_PessoaId",
                table: "Clientes",
                column: "PessoaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_SyncGuid",
                table: "Clientes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteUsosContinuos_ClienteId",
                table: "ClienteUsosContinuos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteUsosContinuos_ProdutoId",
                table: "ClienteUsosContinuos",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClienteAutorizacoes");

            migrationBuilder.DropTable(
                name: "ClienteConvenios");

            migrationBuilder.DropTable(
                name: "ClienteDescontos");

            migrationBuilder.DropTable(
                name: "ClienteUsosContinuos");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
