using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDicionarioRelacionamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DicionarioRelacionamentos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColunaFk = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TabelaAlvo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OnDelete = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "restrict"),
                    OnUpdate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "noAction"),
                    Revisado = table.Column<bool>(type: "boolean", nullable: false),
                    RevisadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DicionarioRelacionamentos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DicionarioRelacionamentos_Tabela_ColunaFk",
                table: "DicionarioRelacionamentos",
                columns: new[] { "Tabela", "ColunaFk" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DicionarioRelacionamentos");
        }
    }
}
