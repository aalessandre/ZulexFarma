using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoLoteRastreabilidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LotesConferidos",
                table: "Compras",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LotesConferidosEm",
                table: "Compras",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LotesConferidosPorUsuarioId",
                table: "Compras",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SngpcOptOut",
                table: "Compras",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComprasProdutosLotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompraProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    NumeroLote = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DataFabricacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataValidade = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    RegistroMs = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NumeroLoteOriginal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DataFabricacaoOriginal = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataValidadeOriginal = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RegistroMsOriginal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    EditadoPeloUsuario = table.Column<bool>(type: "boolean", nullable: false),
                    EditadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EditadoPorUsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprasProdutosLotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprasProdutosLotes_ComprasProdutos_CompraProdutoId",
                        column: x => x.CompraProdutoId,
                        principalTable: "ComprasProdutos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComprasProdutosLotes_Usuarios_EditadoPorUsuarioId",
                        column: x => x.EditadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosLotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    NumeroLote = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DataFabricacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataValidade = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SaldoAtual = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    RegistroMs = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    FornecedorId = table.Column<long>(type: "bigint", nullable: true),
                    CompraId = table.Column<long>(type: "bigint", nullable: true),
                    EhLoteFicticio = table.Column<bool>(type: "boolean", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrimeiraEntradaEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UltimaMovimentacaoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosLotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosLotes_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProdutosLotes_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProdutosLotes_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimentosLote",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoLoteId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    DataMovimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    CompraId = table.Column<long>(type: "bigint", nullable: true),
                    VendaId = table.Column<long>(type: "bigint", nullable: true),
                    CompraProdutoLoteId = table.Column<long>(type: "bigint", nullable: true),
                    SaldoAposMovimento = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
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
                    table.PrimaryKey("PK_MovimentosLote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosLote_ComprasProdutosLotes_CompraProdutoLoteId",
                        column: x => x.CompraProdutoLoteId,
                        principalTable: "ComprasProdutosLotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimentosLote_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimentosLote_ProdutosLotes_ProdutoLoteId",
                        column: x => x.ProdutoLoteId,
                        principalTable: "ProdutosLotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimentosLote_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimentosLote_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutosLotes_Codigo",
                table: "ComprasProdutosLotes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutosLotes_CompraProdutoId",
                table: "ComprasProdutosLotes",
                column: "CompraProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutosLotes_EditadoPorUsuarioId",
                table: "ComprasProdutosLotes",
                column: "EditadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutosLotes_SyncGuid",
                table: "ComprasProdutosLotes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_Codigo",
                table: "MovimentosLote",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_CompraId",
                table: "MovimentosLote",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_CompraProdutoLoteId",
                table: "MovimentosLote",
                column: "CompraProdutoLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_ProdutoLoteId_DataMovimento",
                table: "MovimentosLote",
                columns: new[] { "ProdutoLoteId", "DataMovimento" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_SyncGuid",
                table: "MovimentosLote",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_UsuarioId",
                table: "MovimentosLote",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_VendaId",
                table: "MovimentosLote",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_Codigo",
                table: "ProdutosLotes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_CompraId",
                table: "ProdutosLotes",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_FilialId_ProdutoId_DataValidade",
                table: "ProdutosLotes",
                columns: new[] { "FilialId", "ProdutoId", "DataValidade" });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_FilialId_ProdutoId_NumeroLote",
                table: "ProdutosLotes",
                columns: new[] { "FilialId", "ProdutoId", "NumeroLote" });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_FornecedorId",
                table: "ProdutosLotes",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_ProdutoId",
                table: "ProdutosLotes",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_SyncGuid",
                table: "ProdutosLotes",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentosLote");

            migrationBuilder.DropTable(
                name: "ComprasProdutosLotes");

            migrationBuilder.DropTable(
                name: "ProdutosLotes");

            migrationBuilder.DropColumn(
                name: "LotesConferidos",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "LotesConferidosEm",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "LotesConferidosPorUsuarioId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "SngpcOptOut",
                table: "Compras");
        }
    }
}
