using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanoContas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanosContas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    Natureza = table.Column<int>(type: "integer", nullable: false),
                    ContaPaiId = table.Column<long>(type: "bigint", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    VisivelRelatorio = table.Column<bool>(type: "boolean", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosContas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosContas_PlanosContas_ContaPaiId",
                        column: x => x.ContaPaiId,
                        principalTable: "PlanosContas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanosContas_Codigo",
                table: "PlanosContas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosContas_ContaPaiId",
                table: "PlanosContas",
                column: "ContaPaiId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosContas_SyncGuid",
                table: "PlanosContas",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanosContas");
        }
    }
}
