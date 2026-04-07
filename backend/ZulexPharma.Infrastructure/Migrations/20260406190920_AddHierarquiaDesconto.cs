using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarquiaDesconto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HierarquiaDescontos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Padrao = table.Column<bool>(type: "boolean", nullable: false),
                    AplicarAutomatico = table.Column<bool>(type: "boolean", nullable: false),
                    DescontoAutoTipo = table.Column<int>(type: "integer", nullable: true),
                    BuscarMenorValorPromocao = table.Column<bool>(type: "boolean", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiaDescontos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HierarquiaDescontoClientes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HierarquiaDescontoId = table.Column<long>(type: "bigint", nullable: false),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiaDescontoClientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarquiaDescontoClientes_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HierarquiaDescontoClientes_HierarquiaDescontos_HierarquiaDe~",
                        column: x => x.HierarquiaDescontoId,
                        principalTable: "HierarquiaDescontos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarquiaDescontoColaboradores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HierarquiaDescontoId = table.Column<long>(type: "bigint", nullable: false),
                    ColaboradorId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiaDescontoColaboradores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarquiaDescontoColaboradores_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HierarquiaDescontoColaboradores_HierarquiaDescontos_Hierarq~",
                        column: x => x.HierarquiaDescontoId,
                        principalTable: "HierarquiaDescontos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarquiaDescontoConvenios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HierarquiaDescontoId = table.Column<long>(type: "bigint", nullable: false),
                    ConvenioId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiaDescontoConvenios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarquiaDescontoConvenios_Convenios_ConvenioId",
                        column: x => x.ConvenioId,
                        principalTable: "Convenios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HierarquiaDescontoConvenios_HierarquiaDescontos_HierarquiaD~",
                        column: x => x.HierarquiaDescontoId,
                        principalTable: "HierarquiaDescontos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarquiaDescontoItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HierarquiaDescontoId = table.Column<long>(type: "bigint", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Componente = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiaDescontoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarquiaDescontoItens_HierarquiaDescontos_HierarquiaDesco~",
                        column: x => x.HierarquiaDescontoId,
                        principalTable: "HierarquiaDescontos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarquiaDescontoSecoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HierarquiaDescontoItemId = table.Column<long>(type: "bigint", nullable: false),
                    SecaoId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiaDescontoSecoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarquiaDescontoSecoes_HierarquiaDescontoItens_Hierarquia~",
                        column: x => x.HierarquiaDescontoItemId,
                        principalTable: "HierarquiaDescontoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HierarquiaDescontoSecoes_Secoes_SecaoId",
                        column: x => x.SecaoId,
                        principalTable: "Secoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontoClientes_ClienteId",
                table: "HierarquiaDescontoClientes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontoClientes_HierarquiaDescontoId",
                table: "HierarquiaDescontoClientes",
                column: "HierarquiaDescontoId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontoColaboradores_ColaboradorId",
                table: "HierarquiaDescontoColaboradores",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontoColaboradores_HierarquiaDescontoId",
                table: "HierarquiaDescontoColaboradores",
                column: "HierarquiaDescontoId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontoConvenios_ConvenioId",
                table: "HierarquiaDescontoConvenios",
                column: "ConvenioId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontoConvenios_HierarquiaDescontoId",
                table: "HierarquiaDescontoConvenios",
                column: "HierarquiaDescontoId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontoItens_HierarquiaDescontoId",
                table: "HierarquiaDescontoItens",
                column: "HierarquiaDescontoId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontos_Codigo",
                table: "HierarquiaDescontos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontos_SyncGuid",
                table: "HierarquiaDescontos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontoSecoes_HierarquiaDescontoItemId",
                table: "HierarquiaDescontoSecoes",
                column: "HierarquiaDescontoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontoSecoes_SecaoId",
                table: "HierarquiaDescontoSecoes",
                column: "SecaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HierarquiaDescontoClientes");

            migrationBuilder.DropTable(
                name: "HierarquiaDescontoColaboradores");

            migrationBuilder.DropTable(
                name: "HierarquiaDescontoConvenios");

            migrationBuilder.DropTable(
                name: "HierarquiaDescontoSecoes");

            migrationBuilder.DropTable(
                name: "HierarquiaDescontoItens");

            migrationBuilder.DropTable(
                name: "HierarquiaDescontos");
        }
    }
}
