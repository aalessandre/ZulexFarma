using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncV2FilaAndCodigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "UsuariosGruposPermissao",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "UsuariosGrupos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "UsuarioFilialGrupos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Substancias",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "SubGrupos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Secoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "PessoasEndereco",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "PessoasContato",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Pessoas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "LogsErro",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "LogsAcao",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "GruposProdutos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "GruposPrincipais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Fornecedores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Filiais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Fabricantes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Colaboradores",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SequenciasLocais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ultimo = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenciasLocais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncFila",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Operacao = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: false),
                    RegistroCodigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DadosJson = table.Column<string>(type: "text", nullable: true),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Enviado = table.Column<bool>(type: "boolean", nullable: false),
                    EnviadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Erro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncFila", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SequenciasLocais_Tabela",
                table: "SequenciasLocais",
                column: "Tabela",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncFila_Enviado",
                table: "SyncFila",
                column: "Enviado");

            migrationBuilder.CreateIndex(
                name: "IX_SyncFila_FilialOrigemId",
                table: "SyncFila",
                column: "FilialOrigemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SequenciasLocais");

            migrationBuilder.DropTable(
                name: "SyncFila");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "UsuariosGrupos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Substancias");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "SubGrupos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Secoes");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "PessoasContato");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "LogsErro");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "LogsAcao");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "GruposProdutos");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "GruposPrincipais");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Fabricantes");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Colaboradores");
        }
    }
}
