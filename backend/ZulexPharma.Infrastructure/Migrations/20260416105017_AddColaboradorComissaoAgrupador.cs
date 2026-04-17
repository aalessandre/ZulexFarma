using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColaboradorComissaoAgrupador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ColaboradorComissaoAgrupadores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ColaboradorId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_ColaboradorComissaoAgrupadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColaboradorComissaoAgrupadores_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ColaboradorComissaoAgrupadores_Codigo",
                table: "ColaboradorComissaoAgrupadores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ColaboradorComissaoAgrupadores_ColaboradorId",
                table: "ColaboradorComissaoAgrupadores",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_ColaboradorComissaoAgrupadores_SyncGuid",
                table: "ColaboradorComissaoAgrupadores",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ColaboradorComissaoAgrupadores");
        }
    }
}
