using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteBloqueioAndStandardParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PermiteFidelidade",
                table: "Convenios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BloquearDescontoParcelada",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CobrarJurosAtraso",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VenderSomenteComSenha",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ClienteBloqueios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    TipoPagamentoId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteBloqueios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClienteBloqueios_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClienteBloqueios_TiposPagamento_TipoPagamentoId",
                        column: x => x.TipoPagamentoId,
                        principalTable: "TiposPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClienteBloqueios_ClienteId_TipoPagamentoId",
                table: "ClienteBloqueios",
                columns: new[] { "ClienteId", "TipoPagamentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClienteBloqueios_TipoPagamentoId",
                table: "ClienteBloqueios",
                column: "TipoPagamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClienteBloqueios");

            migrationBuilder.DropColumn(
                name: "PermiteFidelidade",
                table: "Convenios");

            migrationBuilder.DropColumn(
                name: "BloquearDescontoParcelada",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "CobrarJurosAtraso",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "VenderSomenteComSenha",
                table: "Clientes");
        }
    }
}
