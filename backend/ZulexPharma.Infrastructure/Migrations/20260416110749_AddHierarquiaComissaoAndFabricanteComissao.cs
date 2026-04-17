using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarquiaComissaoAndFabricanteComissao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FabricanteComissaoAgrupadores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FabricanteId = table.Column<long>(type: "bigint", nullable: false),
                    TipoAgrupador = table.Column<int>(type: "integer", nullable: false),
                    AgrupadorId = table.Column<long>(type: "bigint", nullable: false),
                    AgrupadorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComissaoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FabricanteComissaoAgrupadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FabricanteComissaoAgrupadores_Fabricantes_FabricanteId",
                        column: x => x.FabricanteId,
                        principalTable: "Fabricantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarquiasComissao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Padrao = table.Column<bool>(type: "boolean", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiasComissao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HierarquiaComissaoColaboradores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HierarquiaComissaoId = table.Column<long>(type: "bigint", nullable: false),
                    ColaboradorId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiaComissaoColaboradores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarquiaComissaoColaboradores_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HierarquiaComissaoColaboradores_HierarquiasComissao_Hierarq~",
                        column: x => x.HierarquiaComissaoId,
                        principalTable: "HierarquiasComissao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarquiaComissaoItens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HierarquiaComissaoId = table.Column<long>(type: "bigint", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Componente = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiaComissaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarquiaComissaoItens_HierarquiasComissao_HierarquiaComis~",
                        column: x => x.HierarquiaComissaoId,
                        principalTable: "HierarquiasComissao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HierarquiaComissaoSecoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HierarquiaComissaoItemId = table.Column<long>(type: "bigint", nullable: false),
                    SecaoId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarquiaComissaoSecoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarquiaComissaoSecoes_HierarquiaComissaoItens_Hierarquia~",
                        column: x => x.HierarquiaComissaoItemId,
                        principalTable: "HierarquiaComissaoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HierarquiaComissaoSecoes_Secoes_SecaoId",
                        column: x => x.SecaoId,
                        principalTable: "Secoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FabricanteComissaoAgrupadores_Codigo",
                table: "FabricanteComissaoAgrupadores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FabricanteComissaoAgrupadores_FabricanteId",
                table: "FabricanteComissaoAgrupadores",
                column: "FabricanteId");

            migrationBuilder.CreateIndex(
                name: "IX_FabricanteComissaoAgrupadores_SyncGuid",
                table: "FabricanteComissaoAgrupadores",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaComissaoColaboradores_ColaboradorId",
                table: "HierarquiaComissaoColaboradores",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaComissaoColaboradores_HierarquiaComissaoId",
                table: "HierarquiaComissaoColaboradores",
                column: "HierarquiaComissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaComissaoItens_HierarquiaComissaoId",
                table: "HierarquiaComissaoItens",
                column: "HierarquiaComissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaComissaoSecoes_HierarquiaComissaoItemId",
                table: "HierarquiaComissaoSecoes",
                column: "HierarquiaComissaoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaComissaoSecoes_SecaoId",
                table: "HierarquiaComissaoSecoes",
                column: "SecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiasComissao_Codigo",
                table: "HierarquiasComissao",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiasComissao_SyncGuid",
                table: "HierarquiasComissao",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FabricanteComissaoAgrupadores");

            migrationBuilder.DropTable(
                name: "HierarquiaComissaoColaboradores");

            migrationBuilder.DropTable(
                name: "HierarquiaComissaoSecoes");

            migrationBuilder.DropTable(
                name: "HierarquiaComissaoItens");

            migrationBuilder.DropTable(
                name: "HierarquiasComissao");
        }
    }
}
