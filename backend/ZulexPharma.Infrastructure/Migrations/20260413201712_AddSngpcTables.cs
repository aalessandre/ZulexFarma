using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSngpcTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventariosSngpc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    DataInventario = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataFinalizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_InventariosSngpc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventariosSngpc_Usuarios_UsuarioId",
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
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoLoteId = table.Column<long>(type: "bigint", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    DataPerda = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Motivo = table.Column<int>(type: "integer", nullable: false),
                    NumeroBoletim = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                name: "Receitas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    VendaId = table.Column<long>(type: "bigint", nullable: true),
                    MedicoNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MedicoCrm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MedicoUf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    MedicoCpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    PacienteNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PacienteCpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    PacienteEndereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PacienteCep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PacienteCidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PacienteUf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    NumeroReceita = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TipoReceita = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
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
                    table.PrimaryKey("PK_Receitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receitas_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SngpcMapas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    CompetenciaMes = table.Column<int>(type: "integer", nullable: false),
                    CompetenciaAno = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataGeracao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataEnvio = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    XmlConteudo = table.Column<string>(type: "text", nullable: true),
                    ProtocoloAnvisa = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TotalEntradas = table.Column<int>(type: "integer", nullable: false),
                    TotalSaidas = table.Column<int>(type: "integer", nullable: false),
                    TotalReceitas = table.Column<int>(type: "integer", nullable: false),
                    TotalPerdas = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_SngpcMapas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SngpcMapas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InventariosSngpcItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventarioSngpcId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    NumeroLote = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DataFabricacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataValidade = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    RegistroMs = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventariosSngpcItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventariosSngpcItens_InventariosSngpc_InventarioSngpcId",
                        column: x => x.InventarioSngpcId,
                        principalTable: "InventariosSngpc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventariosSngpcItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceitasItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReceitaId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoLoteId = table.Column<long>(type: "bigint", nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    Posologia = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceitasItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceitasItens_ProdutosLotes_ProdutoLoteId",
                        column: x => x.ProdutoLoteId,
                        principalTable: "ProdutosLotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReceitasItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceitasItens_Receitas_ReceitaId",
                        column: x => x.ReceitaId,
                        principalTable: "Receitas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpc_Codigo",
                table: "InventariosSngpc",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpc_FilialId_DataInventario",
                table: "InventariosSngpc",
                columns: new[] { "FilialId", "DataInventario" });

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpc_SyncGuid",
                table: "InventariosSngpc",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpc_UsuarioId",
                table: "InventariosSngpc",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpcItens_Codigo",
                table: "InventariosSngpcItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpcItens_InventarioSngpcId",
                table: "InventariosSngpcItens",
                column: "InventarioSngpcId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpcItens_ProdutoId",
                table: "InventariosSngpcItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpcItens_SyncGuid",
                table: "InventariosSngpcItens",
                column: "SyncGuid");

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

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_Codigo",
                table: "Receitas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_FilialId_DataEmissao",
                table: "Receitas",
                columns: new[] { "FilialId", "DataEmissao" });

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_SyncGuid",
                table: "Receitas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_VendaId",
                table: "Receitas",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_Codigo",
                table: "ReceitasItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_ProdutoId",
                table: "ReceitasItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_ProdutoLoteId",
                table: "ReceitasItens",
                column: "ProdutoLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_ReceitaId",
                table: "ReceitasItens",
                column: "ReceitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitasItens_SyncGuid",
                table: "ReceitasItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SngpcMapas_Codigo",
                table: "SngpcMapas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SngpcMapas_FilialId_CompetenciaAno_CompetenciaMes",
                table: "SngpcMapas",
                columns: new[] { "FilialId", "CompetenciaAno", "CompetenciaMes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SngpcMapas_SyncGuid",
                table: "SngpcMapas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SngpcMapas_UsuarioId",
                table: "SngpcMapas",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventariosSngpcItens");

            migrationBuilder.DropTable(
                name: "Perdas");

            migrationBuilder.DropTable(
                name: "ReceitasItens");

            migrationBuilder.DropTable(
                name: "SngpcMapas");

            migrationBuilder.DropTable(
                name: "InventariosSngpc");

            migrationBuilder.DropTable(
                name: "Receitas");
        }
    }
}
