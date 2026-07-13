using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMovimentoEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovimentosEstoque",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoId = table.Column<long>(type: "bigint", nullable: false),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    ProdutoVariacaoId = table.Column<long>(type: "bigint", nullable: true),
                    Data = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    SaldoApos = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    Documento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PessoaId = table.Column<long>(type: "bigint", nullable: true),
                    PessoaNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    CompraId = table.Column<long>(type: "bigint", nullable: true),
                    VendaId = table.Column<long>(type: "bigint", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosEstoque", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_Codigo",
                table: "MovimentosEstoque",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_ProdutoId_FilialId_Data",
                table: "MovimentosEstoque",
                columns: new[] { "ProdutoId", "FilialId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_SyncGuid",
                table: "MovimentosEstoque",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentosEstoque");
        }
    }
}
