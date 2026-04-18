using System;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMunicipios : Migration
    {
        private static string LerSeedSql()
        {
            var asm = Assembly.GetExecutingAssembly();
            var nome = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("municipios_seed.sql"))
                ?? throw new InvalidOperationException("Seed municipios_seed.sql não encontrado como EmbeddedResource.");
            using var stream = asm.GetManifestResourceStream(nome)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MunicipioId",
                table: "PessoasEndereco",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodigoIbgeMunicipio",
                table: "Filiais",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MunicipioId",
                table: "Filiais",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Municipios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoIbge = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NomeNormalizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PessoasEndereco_MunicipioId",
                table: "PessoasEndereco",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_Filiais_MunicipioId",
                table: "Filiais",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_Codigo",
                table: "Municipios",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_CodigoIbge",
                table: "Municipios",
                column: "CodigoIbge",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_SyncGuid",
                table: "Municipios",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_Uf_NomeNormalizado",
                table: "Municipios",
                columns: new[] { "Uf", "NomeNormalizado" });

            migrationBuilder.AddForeignKey(
                name: "FK_Filiais_Municipios_MunicipioId",
                table: "Filiais",
                column: "MunicipioId",
                principalTable: "Municipios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PessoasEndereco_Municipios_MunicipioId",
                table: "PessoasEndereco",
                column: "MunicipioId",
                principalTable: "Municipios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Seed dos 5571 municípios IBGE (carregado de SeedData/municipios_seed.sql via EmbeddedResource)
            migrationBuilder.Sql(LerSeedSql());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Filiais_Municipios_MunicipioId",
                table: "Filiais");

            migrationBuilder.DropForeignKey(
                name: "FK_PessoasEndereco_Municipios_MunicipioId",
                table: "PessoasEndereco");

            migrationBuilder.DropTable(
                name: "Municipios");

            migrationBuilder.DropIndex(
                name: "IX_PessoasEndereco_MunicipioId",
                table: "PessoasEndereco");

            migrationBuilder.DropIndex(
                name: "IX_Filiais_MunicipioId",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "MunicipioId",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "MunicipioId",
                table: "Filiais");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoIbgeMunicipio",
                table: "Filiais",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
