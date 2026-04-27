using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoNaturezaOperacaoNfce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CodigoNaturezaOperacaoNfce",
                table: "SelfCheckoutConfiguracoes",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoNaturezaOperacaoNfce",
                table: "SelfCheckoutConfiguracoes");
        }
    }
}
