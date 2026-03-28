using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDicionarioTabelaAndInstrucaoIA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Replica",
                table: "DicionarioRevisoes");

            migrationBuilder.AddColumn<string>(
                name: "InstrucaoIA",
                table: "DicionarioRevisoes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DicionarioTabelas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Escopo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "global"),
                    Replica = table.Column<bool>(type: "boolean", nullable: false),
                    InstrucaoIA = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DicionarioTabelas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DicionarioTabelas_Tabela",
                table: "DicionarioTabelas",
                column: "Tabela",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DicionarioTabelas");

            migrationBuilder.DropColumn(
                name: "InstrucaoIA",
                table: "DicionarioRevisoes");

            migrationBuilder.AddColumn<bool>(
                name: "Replica",
                table: "DicionarioRevisoes",
                type: "boolean",
                nullable: true);
        }
    }
}
