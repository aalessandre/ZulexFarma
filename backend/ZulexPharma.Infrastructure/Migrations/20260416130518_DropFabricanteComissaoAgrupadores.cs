using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropFabricanteComissaoAgrupadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FabricanteComissaoAgrupadores");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FabricanteComissaoAgrupadores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FabricanteId = table.Column<long>(type: "bigint", nullable: false),
                    AgrupadorId = table.Column<long>(type: "bigint", nullable: false),
                    AgrupadorNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    ComissaoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TipoAgrupador = table.Column<int>(type: "integer", nullable: false)
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
        }
    }
}
