using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfCheckoutFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SelfCheckoutTerminalId",
                table: "Vendas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ProdutoId",
                table: "VendaItens",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "SelfCheckoutConciliacoesEstoque",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaItemId = table.Column<long>(type: "bigint", nullable: false),
                    CodigoProdutoExterno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoBarrasExterno = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    ProcessadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UltimoErro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfCheckoutConciliacoesEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfCheckoutConciliacoesEstoque_VendaItens_VendaItemId",
                        column: x => x.VendaItemId,
                        principalTable: "VendaItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelfCheckoutConfiguracoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    ErpOrigem = table.Column<int>(type: "integer", nullable: false),
                    HostBanco = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NomeBanco = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UsuarioBanco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SenhaBancoCriptografada = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FilialErpOrigem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SerieNfce = table.Column<int>(type: "integer", nullable: false),
                    UsuarioVirtualId = table.Column<long>(type: "bigint", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfCheckoutConfiguracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfCheckoutConfiguracoes_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SelfCheckoutConfiguracoes_Usuarios_UsuarioVirtualId",
                        column: x => x.UsuarioVirtualId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SelfCheckoutTerminais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Apelido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UltimaAtividade = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfCheckoutTerminais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfCheckoutTerminais_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SequenciasCentrais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    ModeloDocumento = table.Column<int>(type: "integer", nullable: false),
                    Serie = table.Column<int>(type: "integer", nullable: false),
                    ProximoNumero = table.Column<long>(type: "bigint", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenciasCentrais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SequenciasCentrais_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SelfCheckoutChamadosAtendente",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TerminalId = table.Column<long>(type: "bigint", nullable: false),
                    Motivo = table.Column<int>(type: "integer", nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AtendidoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AtendidoPorColaboradorId = table.Column<long>(type: "bigint", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfCheckoutChamadosAtendente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfCheckoutChamadosAtendente_Colaboradores_AtendidoPorCola~",
                        column: x => x.AtendidoPorColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SelfCheckoutChamadosAtendente_SelfCheckoutTerminais_Termina~",
                        column: x => x.TerminalId,
                        principalTable: "SelfCheckoutTerminais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_FilialId_Origem",
                table: "Vendas",
                columns: new[] { "FilialId", "Origem" });

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_SelfCheckoutTerminalId",
                table: "Vendas",
                column: "SelfCheckoutTerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutChamadosAtendente_AtendidoPorColaboradorId",
                table: "SelfCheckoutChamadosAtendente",
                column: "AtendidoPorColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutChamadosAtendente_Codigo",
                table: "SelfCheckoutChamadosAtendente",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutChamadosAtendente_SyncGuid",
                table: "SelfCheckoutChamadosAtendente",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutChamadosAtendente_TerminalId_AtendidoEm",
                table: "SelfCheckoutChamadosAtendente",
                columns: new[] { "TerminalId", "AtendidoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_Codigo",
                table: "SelfCheckoutConciliacoesEstoque",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_ProcessadoEm",
                table: "SelfCheckoutConciliacoesEstoque",
                column: "ProcessadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_SyncGuid",
                table: "SelfCheckoutConciliacoesEstoque",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_VendaItemId",
                table: "SelfCheckoutConciliacoesEstoque",
                column: "VendaItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConfiguracoes_Codigo",
                table: "SelfCheckoutConfiguracoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConfiguracoes_FilialId",
                table: "SelfCheckoutConfiguracoes",
                column: "FilialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConfiguracoes_SyncGuid",
                table: "SelfCheckoutConfiguracoes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConfiguracoes_UsuarioVirtualId",
                table: "SelfCheckoutConfiguracoes",
                column: "UsuarioVirtualId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutTerminais_Codigo",
                table: "SelfCheckoutTerminais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutTerminais_FilialId_Numero",
                table: "SelfCheckoutTerminais",
                columns: new[] { "FilialId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutTerminais_SyncGuid",
                table: "SelfCheckoutTerminais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SequenciasCentrais_Codigo",
                table: "SequenciasCentrais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SequenciasCentrais_FilialId_ModeloDocumento_Serie",
                table: "SequenciasCentrais",
                columns: new[] { "FilialId", "ModeloDocumento", "Serie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SequenciasCentrais_SyncGuid",
                table: "SequenciasCentrais",
                column: "SyncGuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Vendas_SelfCheckoutTerminais_SelfCheckoutTerminalId",
                table: "Vendas",
                column: "SelfCheckoutTerminalId",
                principalTable: "SelfCheckoutTerminais",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vendas_SelfCheckoutTerminais_SelfCheckoutTerminalId",
                table: "Vendas");

            migrationBuilder.DropTable(
                name: "SelfCheckoutChamadosAtendente");

            migrationBuilder.DropTable(
                name: "SelfCheckoutConciliacoesEstoque");

            migrationBuilder.DropTable(
                name: "SelfCheckoutConfiguracoes");

            migrationBuilder.DropTable(
                name: "SequenciasCentrais");

            migrationBuilder.DropTable(
                name: "SelfCheckoutTerminais");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_FilialId_Origem",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_SelfCheckoutTerminalId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "SelfCheckoutTerminalId",
                table: "Vendas");

            migrationBuilder.AlterColumn<long>(
                name: "ProdutoId",
                table: "VendaItens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
