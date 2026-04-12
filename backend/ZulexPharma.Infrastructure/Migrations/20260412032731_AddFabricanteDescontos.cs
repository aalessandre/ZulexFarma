using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFabricanteDescontos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DescontoMaximo",
                table: "Fabricantes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DescontoMaximoComSenha",
                table: "Fabricantes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DescontoMinimo",
                table: "Fabricantes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescontoMaximo",
                table: "Fabricantes");

            migrationBuilder.DropColumn(
                name: "DescontoMaximoComSenha",
                table: "Fabricantes");

            migrationBuilder.DropColumn(
                name: "DescontoMinimo",
                table: "Fabricantes");
        }
    }
}
