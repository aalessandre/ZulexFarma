using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DescontoGeral",
                table: "Convenios",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiasCarenciaBloqueio",
                table: "Clientes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescontoGeral",
                table: "Convenios");

            migrationBuilder.DropColumn(
                name: "DiasCarenciaBloqueio",
                table: "Clientes");
        }
    }
}
