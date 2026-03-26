using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiFilialInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "UsuariosGruposPermissao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "UsuariosGruposPermissao",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "UsuariosGrupos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "UsuariosGrupos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "Usuarios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Usuarios",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "UsuarioFilialGrupos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "UsuarioFilialGrupos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "PessoasEndereco",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "PessoasEndereco",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "PessoasContato",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "PessoasContato",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "Pessoas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Pessoas",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "LogsErro",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "LogsErro",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "LogsAcao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "LogsAcao",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "Filiais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VersaoSync",
                table: "Filiais",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FilialOrigemId",
                table: "Colaboradores",
                type: "bigint",
                nullable: true);

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
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UltimaVersaoRecebida = table.Column<long>(type: "bigint", nullable: false),
                    UltimaVersaoEnviada = table.Column<long>(type: "bigint", nullable: false),
                    UltimoSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MensagemErro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncControles");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "UsuariosGrupos");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "UsuariosGrupos");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "PessoasContato");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "PessoasContato");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "LogsErro");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "LogsErro");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "LogsAcao");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "LogsAcao");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "FilialOrigemId",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "VersaoSync",
                table: "Colaboradores");
        }
    }
}
