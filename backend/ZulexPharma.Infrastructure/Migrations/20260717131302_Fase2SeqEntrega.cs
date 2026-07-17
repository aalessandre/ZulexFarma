using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase2SeqEntrega : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FilialDonoId",
                table: "SyncQuarentena",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpUid",
                table: "SyncQuarentena",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SeqEntrega",
                table: "SyncFila",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncFila_SeqEntrega",
                table: "SyncFila",
                column: "SeqEntrega",
                filter: "\"SeqEntrega\" IS NOT NULL");

            // Sequence do publicador (fase 2): numera SeqEntrega so' em linha COMMITADA.
            // Existe em todos os nos (inofensiva no edge — so' o hub numera).
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS seq_sync_entrega;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncFila_SeqEntrega",
                table: "SyncFila");

            migrationBuilder.DropColumn(
                name: "FilialDonoId",
                table: "SyncQuarentena");

            migrationBuilder.DropColumn(
                name: "OpUid",
                table: "SyncQuarentena");

            migrationBuilder.DropColumn(
                name: "SeqEntrega",
                table: "SyncFila");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS seq_sync_entrega;");
        }
    }
}
