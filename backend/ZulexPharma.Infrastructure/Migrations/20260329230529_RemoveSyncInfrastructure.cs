using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSyncInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncControles");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "UsuariosGrupos");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Substancias");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "SubGrupos");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Secoes");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "PessoasContato");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "LogsErro");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "LogsAcao");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "GruposProdutos");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "GruposPrincipais");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Fabricantes");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Colaboradores");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "UsuariosGruposPermissao",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "UsuariosGrupos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Usuarios",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "UsuarioFilialGrupos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Substancias",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "SubGrupos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Secoes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "PessoasEndereco",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "PessoasContato",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Pessoas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "LogsErro",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "LogsAcao",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "GruposProdutos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "GruposPrincipais",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Fornecedores",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Filiais",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Fabricantes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Colaboradores",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "SyncControles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    MensagemErro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UltimaVersaoEnviada = table.Column<long>(type: "bigint", nullable: false),
                    UltimaVersaoRecebida = table.Column<long>(type: "bigint", nullable: false),
                    UltimoSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncControles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncControles_FilialId_Tabela",
                table: "SyncControles",
                columns: new[] { "FilialId", "Tabela" },
                unique: true);
        }
    }
}
