using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSngpcVendaReceita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SngpcPendente",
                table: "Vendas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Prescritores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoConselho = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NumeroConselho = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    Especialidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescritores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendaReceitas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    NumeroNotificacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataValidade = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Cid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PrescritorId = table.Column<long>(type: "bigint", nullable: false),
                    PacienteNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PacienteCpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    PacienteRg = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PacienteNascimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PacienteSexo = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    PacienteEndereco = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PacienteNumero = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PacienteBairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PacienteCidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PacienteUf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    PacienteCep = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    PacienteTelefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CompradorMesmoPaciente = table.Column<bool>(type: "boolean", nullable: false),
                    CompradorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompradorCpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    CompradorRg = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CompradorEndereco = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompradorNumero = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CompradorBairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompradorCidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompradorUf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    CompradorCep = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    CompradorTelefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaReceitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaReceitas_Prescritores_PrescritorId",
                        column: x => x.PrescritorId,
                        principalTable: "Prescritores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendaReceitas_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendaReceitaItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaReceitaId = table.Column<long>(type: "bigint", nullable: false),
                    VendaItemId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoLoteId = table.Column<long>(type: "bigint", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaReceitaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaReceitaItens_ProdutosLotes_ProdutoLoteId",
                        column: x => x.ProdutoLoteId,
                        principalTable: "ProdutosLotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendaReceitaItens_VendaItens_VendaItemId",
                        column: x => x.VendaItemId,
                        principalTable: "VendaItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendaReceitaItens_VendaReceitas_VendaReceitaId",
                        column: x => x.VendaReceitaId,
                        principalTable: "VendaReceitas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prescritores_Codigo",
                table: "Prescritores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Prescritores_Nome",
                table: "Prescritores",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Prescritores_SyncGuid",
                table: "Prescritores",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Prescritores_TipoConselho_NumeroConselho_Uf",
                table: "Prescritores",
                columns: new[] { "TipoConselho", "NumeroConselho", "Uf" });

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_Codigo",
                table: "VendaReceitaItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_ProdutoLoteId",
                table: "VendaReceitaItens",
                column: "ProdutoLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_SyncGuid",
                table: "VendaReceitaItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_VendaItemId",
                table: "VendaReceitaItens",
                column: "VendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_VendaReceitaId",
                table: "VendaReceitaItens",
                column: "VendaReceitaId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitas_Codigo",
                table: "VendaReceitas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitas_PrescritorId",
                table: "VendaReceitas",
                column: "PrescritorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitas_SyncGuid",
                table: "VendaReceitas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitas_VendaId",
                table: "VendaReceitas",
                column: "VendaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendaReceitaItens");

            migrationBuilder.DropTable(
                name: "VendaReceitas");

            migrationBuilder.DropTable(
                name: "Prescritores");

            migrationBuilder.DropColumn(
                name: "SngpcPendente",
                table: "Vendas");
        }
    }
}
