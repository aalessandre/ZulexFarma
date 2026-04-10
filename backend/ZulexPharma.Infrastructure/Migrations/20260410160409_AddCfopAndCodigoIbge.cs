using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCfopAndCodigoIbge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cfop",
                table: "ProdutosFiscal",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoIbgeMunicipio",
                table: "Filiais",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cfop",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "CodigoIbgeMunicipio",
                table: "Filiais");
        }
    }
}
