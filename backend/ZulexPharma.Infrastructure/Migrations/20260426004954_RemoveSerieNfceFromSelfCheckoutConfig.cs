using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSerieNfceFromSelfCheckoutConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerieNfce",
                table: "SelfCheckoutConfiguracoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SerieNfce",
                table: "SelfCheckoutConfiguracoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
