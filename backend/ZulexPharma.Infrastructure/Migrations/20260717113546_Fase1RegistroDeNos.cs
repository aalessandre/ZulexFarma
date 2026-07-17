using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase1RegistroDeNos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncEstadoLocal",
                columns: table => new
                {
                    Chave = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Valor = table.Column<string>(type: "text", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncEstadoLocal", x => x.Chave);
                });

            migrationBuilder.CreateTable(
                name: "SyncNos",
                columns: table => new
                {
                    NoCodigo = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    InstanciaUid = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChaveHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UltimoAckSeq = table.Column<long>(type: "bigint", nullable: false),
                    UltimoPushEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UltimoPullEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    VersaoApp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncNos", x => x.NoCodigo);
                });

            migrationBuilder.CreateTable(
                name: "SyncNoFiliais",
                columns: table => new
                {
                    NoCodigo = table.Column<int>(type: "integer", nullable: false),
                    FilialId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncNoFiliais", x => new { x.NoCodigo, x.FilialId });
                    table.ForeignKey(
                        name: "FK_SyncNoFiliais_SyncNos_NoCodigo",
                        column: x => x.NoCodigo,
                        principalTable: "SyncNos",
                        principalColumn: "NoCodigo",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncEstadoLocal");

            migrationBuilder.DropTable(
                name: "SyncNoFiliais");

            migrationBuilder.DropTable(
                name: "SyncNos");
        }
    }
}
