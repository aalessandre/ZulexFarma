using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRamoVisibilidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RamosVisibilidade",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ramo = table.Column<int>(type: "integer", nullable: false),
                    ElementoId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Visivel = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RamosVisibilidade", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RamosVisibilidade_Ramo_ElementoId",
                table: "RamosVisibilidade",
                columns: new[] { "Ramo", "ElementoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RamosVisibilidade");
        }
    }
}
