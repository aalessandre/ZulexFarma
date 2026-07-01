using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFilialRamo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue 1 = RamoFilial.Farmacia — filiais existentes viram Farmácia (retrocompat).
            migrationBuilder.AddColumn<int>(
                name: "Ramo",
                table: "Filiais",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ramo",
                table: "Filiais");
        }
    }
}
