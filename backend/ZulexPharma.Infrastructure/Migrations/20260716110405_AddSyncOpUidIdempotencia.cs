using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncOpUidIdempotencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OpUid",
                table: "SyncFila",
                type: "uuid",
                nullable: true);

            // BACKFILL do backlog: o outbox so' carimba OpUid em op NOVA, entao as ops AINDA NAO ENVIADAS
            // (ex.: no ficou sem rede e acumulou 300 linhas) subiriam sem chave — e um reenvio delas
            // duplicaria a redistribuicao JUSTO na janela de rollout (restart/deploy e' quando o PUSH mais
            // e' cortado no meio, que e' exatamente o caso que essa feature existe pra cobrir).
            // SEGURO: linha !Enviado nunca foi redistribuida sob esse Guid, entao nao ha' risco de a central
            // descartar op NOVA achando que e' duplicata. (Na central as linhas de redistribuicao tambem sao
            // !Enviado e pegam um Guid aleatorio: inofensivo — ela nao faz PUSH e o /receber nao expoe OpUid.)
            migrationBuilder.Sql(@"UPDATE ""SyncFila"" SET ""OpUid"" = gen_random_uuid() WHERE ""OpUid"" IS NULL AND ""Enviado"" = false;");

            migrationBuilder.CreateIndex(
                name: "IX_SyncFila_OpUid",
                table: "SyncFila",
                column: "OpUid",
                unique: true,
                filter: "\"OpUid\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncFila_OpUid",
                table: "SyncFila");

            migrationBuilder.DropColumn(
                name: "OpUid",
                table: "SyncFila");
        }
    }
}
