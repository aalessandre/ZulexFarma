using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComissaoFaixasDesconto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComissaoFaixasDesconto",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoEntidade = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntidadeId = table.Column<long>(type: "bigint", nullable: false),
                    DescontoInicial = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoFinal = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ComissaoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComissaoFaixasDesconto", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComissaoFaixasDesconto_Codigo",
                table: "ComissaoFaixasDesconto",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComissaoFaixasDesconto_SyncGuid",
                table: "ComissaoFaixasDesconto",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ComissaoFaixasDesconto_TipoEntidade_EntidadeId",
                table: "ComissaoFaixasDesconto",
                columns: new[] { "TipoEntidade", "EntidadeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComissaoFaixasDesconto");
        }
    }
}
