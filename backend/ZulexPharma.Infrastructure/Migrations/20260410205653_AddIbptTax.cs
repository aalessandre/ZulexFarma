using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIbptTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IbptTaxes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ncm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Ex = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AliqNacional = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    AliqImportado = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    AliqEstadual = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    AliqMunicipal = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    VigenciaInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VigenciaFim = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Chave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Versao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fonte = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IbptTaxes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IbptTax_Ncm_Uf_Ex",
                table: "IbptTaxes",
                columns: new[] { "Ncm", "Uf", "Ex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IbptTaxes");
        }
    }
}
