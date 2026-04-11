using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendaDatas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataEmissaoCupom",
                table: "Vendas",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFinalizacao",
                table: "Vendas",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataPreVenda",
                table: "Vendas",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataEmissaoCupom",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "DataFinalizacao",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "DataPreVenda",
                table: "Vendas");
        }
    }
}
