using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreVendaItens");

            migrationBuilder.DropTable(
                name: "PreVendas");

            migrationBuilder.AddColumn<long>(
                name: "PlanoContaId",
                table: "TiposPagamento",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PermitirConferenciaDigitando",
                table: "Produtos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PermitirAbrirCaixa",
                table: "Colaboradores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Caixas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    ColaboradorId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    DataAbertura = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataFechamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataConferencia = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValorAbertura = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_Caixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Caixas_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caixas_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caixas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Vendas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    CaixaId = table.Column<long>(type: "bigint", nullable: true),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true),
                    ColaboradorId = table.Column<long>(type: "bigint", nullable: true),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: true),
                    ConvenioId = table.Column<long>(type: "bigint", nullable: true),
                    NrCesta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Origem = table.Column<int>(type: "integer", nullable: false),
                    TotalBruto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDesconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalLiquido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalItens = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Vendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendas_Caixas_CaixaId",
                        column: x => x.CaixaId,
                        principalTable: "Caixas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vendas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vendas_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vendas_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vendas_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VendaItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoCodigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProdutoNome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Fabricante = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PrecoVenda = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    PercentualDesconto = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    PercentualPromocao = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    ColaboradorId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaItens_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendaItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendaItens_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendaPagamentos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaId = table.Column<long>(type: "bigint", nullable: false),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Troco = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TrocoPara = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaPagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaPagamentos_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendaPagamentos_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendaItemDescontos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaItemId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Percentual = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    Origem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Regra = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrigemId = table.Column<long>(type: "bigint", nullable: true),
                    LiberadoPorId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaItemDescontos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaItemDescontos_Colaboradores_LiberadoPorId",
                        column: x => x.LiberadoPorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendaItemDescontos_VendaItens_VendaItemId",
                        column: x => x.VendaItemId,
                        principalTable: "VendaItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TiposPagamento_PlanoContaId",
                table: "TiposPagamento",
                column: "PlanoContaId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_Codigo",
                table: "Caixas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_ColaboradorId",
                table: "Caixas",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_FilialId",
                table: "Caixas",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_Status",
                table: "Caixas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_SyncGuid",
                table: "Caixas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_UsuarioId",
                table: "Caixas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItemDescontos_LiberadoPorId",
                table: "VendaItemDescontos",
                column: "LiberadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItemDescontos_VendaItemId",
                table: "VendaItemDescontos",
                column: "VendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItens_ColaboradorId",
                table: "VendaItens",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItens_ProdutoId",
                table: "VendaItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItens_VendaId",
                table: "VendaItens",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaPagamentos_TipoPagamentoId",
                table: "VendaPagamentos",
                column: "TipoPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaPagamentos_VendaId",
                table: "VendaPagamentos",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_CaixaId",
                table: "Vendas",
                column: "CaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_ClienteId",
                table: "Vendas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Codigo",
                table: "Vendas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_ColaboradorId",
                table: "Vendas",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId",
                table: "Vendas",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_NrCesta",
                table: "Vendas",
                column: "NrCesta");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Status",
                table: "Vendas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_SyncGuid",
                table: "Vendas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_TipoPagamentoId",
                table: "Vendas",
                column: "TipoPagamentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_TiposPagamento_PlanosContas_PlanoContaId",
                table: "TiposPagamento",
                column: "PlanoContaId",
                principalTable: "PlanosContas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TiposPagamento_PlanosContas_PlanoContaId",
                table: "TiposPagamento");

            migrationBuilder.DropTable(
                name: "VendaItemDescontos");

            migrationBuilder.DropTable(
                name: "VendaPagamentos");

            migrationBuilder.DropTable(
                name: "VendaItens");

            migrationBuilder.DropTable(
                name: "Vendas");

            migrationBuilder.DropTable(
                name: "Caixas");

            migrationBuilder.DropIndex(
                name: "IX_TiposPagamento_PlanoContaId",
                table: "TiposPagamento");

            migrationBuilder.DropColumn(
                name: "PlanoContaId",
                table: "TiposPagamento");

            migrationBuilder.DropColumn(
                name: "PermitirConferenciaDigitando",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "PermitirAbrirCaixa",
                table: "Colaboradores");

            migrationBuilder.CreateTable(
                name: "PreVendas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: true),
                    ColaboradorId = table.Column<long>(type: "bigint", nullable: true),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    ConvenioId = table.Column<long>(type: "bigint", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TotalBruto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDesconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalItens = table.Column<int>(type: "integer", nullable: false),
                    TotalLiquido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreVendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreVendas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PreVendas_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PreVendas_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreVendas_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PreVendaItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PreVendaId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    Fabricante = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    PercentualDesconto = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecoVenda = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProdutoCodigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProdutoNome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreVendaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreVendaItens_PreVendas_PreVendaId",
                        column: x => x.PreVendaId,
                        principalTable: "PreVendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreVendaItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreVendaItens_PreVendaId",
                table: "PreVendaItens",
                column: "PreVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendaItens_ProdutoId",
                table: "PreVendaItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_ClienteId",
                table: "PreVendas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_Codigo",
                table: "PreVendas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_ColaboradorId",
                table: "PreVendas",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_FilialId",
                table: "PreVendas",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_Status",
                table: "PreVendas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_SyncGuid",
                table: "PreVendas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_PreVendas_TipoPagamentoId",
                table: "PreVendas",
                column: "TipoPagamentoId");
        }
    }
}
