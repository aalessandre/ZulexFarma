using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PromocaoLancarPorQuantidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataInicioContagem",
                table: "PromocaoProdutos");

            migrationBuilder.DropColumn(
                name: "LancarPorQuantidade",
                table: "PromocaoProdutos");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInicioContagem",
                table: "Promocoes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LancarPorQuantidade",
                table: "Promocoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataInicioContagem",
                table: "Promocoes");

            migrationBuilder.DropColumn(
                name: "LancarPorQuantidade",
                table: "Promocoes");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInicioContagem",
                table: "PromocaoProdutos",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LancarPorQuantidade",
                table: "PromocaoProdutos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
