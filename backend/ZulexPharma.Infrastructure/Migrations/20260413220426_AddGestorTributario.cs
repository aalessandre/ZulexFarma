using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGestorTributario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaFcp",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaFcpSt",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIcmsSt",
                table: "ProdutosFiscal",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoGestorTributarioEm",
                table: "ProdutosFiscal",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AtualizadoGestorTributarioProvider",
                table: "ProdutosFiscal",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoBeneficio",
                table: "ProdutosFiscal",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispositivoLegalIcms",
                table: "ProdutosFiscal",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnquadramentoIpi",
                table: "ProdutosFiscal",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModBc",
                table: "ProdutosFiscal",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MvaAjustado12",
                table: "ProdutosFiscal",
                type: "numeric(7,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MvaAjustado4",
                table: "ProdutosFiscal",
                type: "numeric(7,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MvaAjustado7",
                table: "ProdutosFiscal",
                type: "numeric(7,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MvaOriginal",
                table: "ProdutosFiscal",
                type: "numeric(7,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NaturezaReceita",
                table: "ProdutosFiscal",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentualReducaoBc",
                table: "ProdutosFiscal",
                type: "numeric(6,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "GestorTributarioJobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DataInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataFim = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TotalItens = table.Column<int>(type: "integer", nullable: false),
                    ItensProcessados = table.Column<int>(type: "integer", nullable: false),
                    ItensAtualizados = table.Column<int>(type: "integer", nullable: false),
                    ItensNaoEncontrados = table.Column<int>(type: "integer", nullable: false),
                    ItensComErro = table.Column<int>(type: "integer", nullable: false),
                    RequisicoesUsadas = table.Column<int>(type: "integer", nullable: false),
                    FiltroJson = table.Column<string>(type: "text", nullable: true),
                    MensagemErro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GestorTributarioJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GestorTributarioJobs_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GestorTributarioUsoMensais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ano = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequisicoesUsadas = table.Column<int>(type: "integer", nullable: false),
                    RequisicoesRevisao = table.Column<int>(type: "integer", nullable: false),
                    RequisicoesAtualizacao = table.Column<int>(type: "integer", nullable: false),
                    RequisicoesDifal = table.Column<int>(type: "integer", nullable: false),
                    UltimaChamadaEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GestorTributarioUsoMensais", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioJobs_Codigo",
                table: "GestorTributarioJobs",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioJobs_Status_CriadoEm",
                table: "GestorTributarioJobs",
                columns: new[] { "Status", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioJobs_SyncGuid",
                table: "GestorTributarioJobs",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioJobs_UsuarioId",
                table: "GestorTributarioJobs",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioUsoMensais_Ano_Mes_Provider",
                table: "GestorTributarioUsoMensais",
                columns: new[] { "Ano", "Mes", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioUsoMensais_Codigo",
                table: "GestorTributarioUsoMensais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioUsoMensais_SyncGuid",
                table: "GestorTributarioUsoMensais",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GestorTributarioJobs");

            migrationBuilder.DropTable(
                name: "GestorTributarioUsoMensais");

            migrationBuilder.DropColumn(
                name: "AliquotaFcp",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaFcpSt",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AliquotaIcmsSt",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AtualizadoGestorTributarioEm",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AtualizadoGestorTributarioProvider",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "CodigoBeneficio",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "DispositivoLegalIcms",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "EnquadramentoIpi",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "ModBc",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "MvaAjustado12",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "MvaAjustado4",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "MvaAjustado7",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "MvaOriginal",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "NaturezaReceita",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "PercentualReducaoBc",
                table: "ProdutosFiscal");
        }
    }
}
