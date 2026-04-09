using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaixaVendaFieldsAndColaboradorItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CaixaId",
                table: "Vendas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NrCesta",
                table: "Vendas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Origem",
                table: "Vendas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "ColaboradorId",
                table: "VendaItens",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Caixas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    ColaboradorId = table.Column<long>(type: "bigint", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    DataAbertura = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataFechamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataConferencia = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValorAbertura = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Caixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Caixas_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caixas_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Caixas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_CaixaId",
                table: "Vendas",
                column: "CaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_NrCesta",
                table: "Vendas",
                column: "NrCesta");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItens_ColaboradorId",
                table: "VendaItens",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_Codigo",
                table: "Caixas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_ColaboradorId",
                table: "Caixas",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_FilialId",
                table: "Caixas",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_Status",
                table: "Caixas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_SyncGuid",
                table: "Caixas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_UsuarioId",
                table: "Caixas",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendaItens_Colaboradores_ColaboradorId",
                table: "VendaItens",
                column: "ColaboradorId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_Caixas_CaixaId",
                table: "Vendas",
                column: "CaixaId",
                principalTable: "Caixas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendaItens_Colaboradores_ColaboradorId",
                table: "VendaItens");

            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_Caixas_CaixaId",
                table: "Vendas");

            migrationBuilder.DropTable(
                name: "Caixas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_CaixaId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_NrCesta",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_VendaItens_ColaboradorId",
                table: "VendaItens");

            migrationBuilder.DropColumn(
                name: "CaixaId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "NrCesta",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "Origem",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "ColaboradorId",
                table: "VendaItens");
        }
    }
}
