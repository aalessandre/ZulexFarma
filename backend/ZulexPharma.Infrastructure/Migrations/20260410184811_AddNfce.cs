using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNfce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nfces",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    VendaId = table.Column<long>(type: "bigint", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    Protocolo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DataAutorizacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ambiente = table.Column<int>(type: "integer", nullable: false),
                    CodigoStatus = table.Column<int>(type: "integer", nullable: false),
                    MotivoStatus = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    XmlEnvio = table.Column<string>(type: "text", nullable: true),
                    XmlRetorno = table.Column<string>(type: "text", nullable: true),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nfces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nfces_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Nfces_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_ChaveAcesso",
                table: "Nfces",
                column: "ChaveAcesso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_Codigo",
                table: "Nfces",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_FilialId_Serie_Numero",
                table: "Nfces",
                columns: new[] { "FilialId", "Serie", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_SyncGuid",
                table: "Nfces",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Nfces_VendaId",
                table: "Nfces",
                column: "VendaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nfces");
        }
    }
}
