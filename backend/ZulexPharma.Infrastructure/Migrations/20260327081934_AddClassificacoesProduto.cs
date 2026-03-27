using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificacoesProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GruposPrincipais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    VersaoSync = table.Column<long>(type: "bigint", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComissaoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMinimo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaximo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaximoComSenha = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProjecaoLucro = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MarkupPadrao = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Priorizar = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ControlarLotesVencimento = table.Column<bool>(type: "boolean", nullable: false),
                    InformarPrescritorVenda = table.Column<bool>(type: "boolean", nullable: false),
                    ImprimirEtiqueta = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirDescontoPrazo = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirPromocao = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirDescontosProgressivos = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposPrincipais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GruposProdutos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    VersaoSync = table.Column<long>(type: "bigint", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComissaoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMinimo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaximo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaximoComSenha = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProjecaoLucro = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MarkupPadrao = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Priorizar = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ControlarLotesVencimento = table.Column<bool>(type: "boolean", nullable: false),
                    InformarPrescritorVenda = table.Column<bool>(type: "boolean", nullable: false),
                    ImprimirEtiqueta = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirDescontoPrazo = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirPromocao = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirDescontosProgressivos = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposProdutos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Secoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    VersaoSync = table.Column<long>(type: "bigint", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComissaoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMinimo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaximo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaximoComSenha = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProjecaoLucro = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MarkupPadrao = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Priorizar = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ControlarLotesVencimento = table.Column<bool>(type: "boolean", nullable: false),
                    InformarPrescritorVenda = table.Column<bool>(type: "boolean", nullable: false),
                    ImprimirEtiqueta = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirDescontoPrazo = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirPromocao = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirDescontosProgressivos = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubGrupos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    VersaoSync = table.Column<long>(type: "bigint", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComissaoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMinimo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaximo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DescontoMaximoComSenha = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ProjecaoLucro = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MarkupPadrao = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Priorizar = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ControlarLotesVencimento = table.Column<bool>(type: "boolean", nullable: false),
                    InformarPrescritorVenda = table.Column<bool>(type: "boolean", nullable: false),
                    ImprimirEtiqueta = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirDescontoPrazo = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirPromocao = table.Column<bool>(type: "boolean", nullable: false),
                    PermitirDescontosProgressivos = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubGrupos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GruposPrincipais");

            migrationBuilder.DropTable(
                name: "GruposProdutos");

            migrationBuilder.DropTable(
                name: "Secoes");

            migrationBuilder.DropTable(
                name: "SubGrupos");
        }
    }
}
