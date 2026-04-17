using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashbackItemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PercentualCashbackItem",
                table: "CampanhasFidelidadeItens",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCashbackItem",
                table: "CampanhasFidelidadeItens",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorVendaReferencia",
                table: "CampanhasFidelidadeItens",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PercentualCashbackItem",
                table: "CampanhasFidelidadeItens");

            migrationBuilder.DropColumn(
                name: "ValorCashbackItem",
                table: "CampanhasFidelidadeItens");

            migrationBuilder.DropColumn(
                name: "ValorVendaReferencia",
                table: "CampanhasFidelidadeItens");
        }
    }
}
