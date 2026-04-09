using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePreVendaToVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename tables
            migrationBuilder.RenameTable(name: "PreVendas", newName: "Vendas");
            migrationBuilder.RenameTable(name: "PreVendaItens", newName: "VendaItens");

            // Rename FK column in VendaItens
            migrationBuilder.RenameColumn(name: "PreVendaId", table: "VendaItens", newName: "VendaId");
            migrationBuilder.RenameIndex(name: "IX_PreVendaItens_PreVendaId", table: "VendaItens", newName: "IX_VendaItens_VendaId");
            migrationBuilder.RenameIndex(name: "IX_PreVendaItens_ProdutoId", table: "VendaItens", newName: "IX_VendaItens_ProdutoId");

            // Rename indexes on Vendas
            migrationBuilder.RenameIndex(name: "IX_PreVendas_ClienteId", table: "Vendas", newName: "IX_Vendas_ClienteId");
            migrationBuilder.RenameIndex(name: "IX_PreVendas_Codigo", table: "Vendas", newName: "IX_Vendas_Codigo");
            migrationBuilder.RenameIndex(name: "IX_PreVendas_ColaboradorId", table: "Vendas", newName: "IX_Vendas_ColaboradorId");
            migrationBuilder.RenameIndex(name: "IX_PreVendas_FilialId", table: "Vendas", newName: "IX_Vendas_FilialId");
            migrationBuilder.RenameIndex(name: "IX_PreVendas_Status", table: "Vendas", newName: "IX_Vendas_Status");
            migrationBuilder.RenameIndex(name: "IX_PreVendas_SyncGuid", table: "Vendas", newName: "IX_Vendas_SyncGuid");
            migrationBuilder.RenameIndex(name: "IX_PreVendas_TipoPagamentoId", table: "Vendas", newName: "IX_Vendas_TipoPagamentoId");

            // Add new column PercentualPromocao to VendaItens
            migrationBuilder.AddColumn<decimal>(
                name: "PercentualPromocao", table: "VendaItens", type: "numeric(8,4)", precision: 8, scale: 4, nullable: false, defaultValue: 0m);

            // Create new table VendaItemDescontos
            migrationBuilder.CreateTable(
                name: "VendaItemDescontos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaItemId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Percentual = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    Origem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Regra = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrigemId = table.Column<long>(type: "bigint", nullable: true),
                    LiberadoPorId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaItemDescontos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaItemDescontos_Colaboradores_LiberadoPorId",
                        column: x => x.LiberadoPorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendaItemDescontos_VendaItens_VendaItemId",
                        column: x => x.VendaItemId,
                        principalTable: "VendaItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_VendaItemDescontos_LiberadoPorId", table: "VendaItemDescontos", column: "LiberadoPorId");
            migrationBuilder.CreateIndex(name: "IX_VendaItemDescontos_VendaItemId", table: "VendaItemDescontos", column: "VendaItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VendaItemDescontos");
            migrationBuilder.DropColumn(name: "PercentualPromocao", table: "VendaItens");

            migrationBuilder.RenameColumn(name: "VendaId", table: "VendaItens", newName: "PreVendaId");
            migrationBuilder.RenameTable(name: "VendaItens", newName: "PreVendaItens");
            migrationBuilder.RenameTable(name: "Vendas", newName: "PreVendas");
        }
    }
}
