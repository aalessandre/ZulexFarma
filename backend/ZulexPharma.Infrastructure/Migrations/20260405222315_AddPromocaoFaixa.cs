using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromocaoFaixa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Intersabores",
                table: "Promocoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PromocaoFaixas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromocaoId = table.Column<long>(type: "bigint", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    PercentualDesconto = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocaoFaixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromocaoFaixas_Promocoes_PromocaoId",
                        column: x => x.PromocaoId,
                        principalTable: "Promocoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromocaoFaixas_PromocaoId",
                table: "PromocaoFaixas",
                column: "PromocaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromocaoFaixas");

            migrationBuilder.DropColumn(
                name: "Intersabores",
                table: "Promocoes");
        }
    }
}
