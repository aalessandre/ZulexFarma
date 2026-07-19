using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase2bIndicePublicador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SyncFila_SeqEntrega_Pendente",
                table: "SyncFila",
                column: "SeqEntrega",
                filter: "\"SeqEntrega\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncFila_SeqEntrega_Pendente",
                table: "SyncFila");
        }
    }
}
