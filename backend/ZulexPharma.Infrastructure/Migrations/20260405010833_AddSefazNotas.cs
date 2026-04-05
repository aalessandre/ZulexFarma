using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSefazNotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SefazNotas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    ChaveNfe = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    Nsu = table.Column<long>(type: "bigint", nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    RazaoSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NumeroNf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SerieNf = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValorNota = table.Column<decimal>(type: "numeric", nullable: false),
                    Situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TipoDocumento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    XmlCompleto = table.Column<string>(type: "text", nullable: true),
                    Manifestada = table.Column<bool>(type: "boolean", nullable: false),
                    TipoManifestacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Importada = table.Column<bool>(type: "boolean", nullable: false),
                    Lancada = table.Column<bool>(type: "boolean", nullable: false),
                    ConsultadaEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SefazNotas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SefazNotas_FilialId",
                table: "SefazNotas",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_SefazNotas_FilialId_ChaveNfe",
                table: "SefazNotas",
                columns: new[] { "FilialId", "ChaveNfe" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SefazNotas");
        }
    }
}
