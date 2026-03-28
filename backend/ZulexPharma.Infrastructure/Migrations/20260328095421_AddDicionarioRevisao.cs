using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDicionarioRevisao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DicionarioRevisoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Coluna = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Revisado = table.Column<bool>(type: "boolean", nullable: false),
                    Unico = table.Column<bool>(type: "boolean", nullable: true),
                    Obrigatorio = table.Column<bool>(type: "boolean", nullable: true),
                    Replica = table.Column<bool>(type: "boolean", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RevisadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DicionarioRevisoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DicionarioRevisoes_Tabela_Coluna",
                table: "DicionarioRevisoes",
                columns: new[] { "Tabela", "Coluna" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DicionarioRevisoes");
        }
    }
}
