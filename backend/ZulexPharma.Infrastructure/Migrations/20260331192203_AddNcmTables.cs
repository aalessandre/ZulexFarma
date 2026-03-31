using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNcmTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ncms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoNcm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExTipi = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    UnidadeTributavel = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ncms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NcmFederais",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NcmId = table.Column<long>(type: "bigint", nullable: false),
                    AliquotaIi = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AliquotaIpi = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CstIpi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    AliquotaPis = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CstPis = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    AliquotaCofins = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CstCofins = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    VigenciaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VigenciaFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NcmFederais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NcmFederais_Ncms_NcmId",
                        column: x => x.NcmId,
                        principalTable: "Ncms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NcmIcmsUfs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NcmId = table.Column<long>(type: "bigint", nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CstIcms = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Csosn = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    AliquotaIcms = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ReducaoBaseCalculo = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AliquotaFcp = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Cbenef = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    VigenciaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VigenciaFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NcmIcmsUfs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NcmIcmsUfs_Ncms_NcmId",
                        column: x => x.NcmId,
                        principalTable: "Ncms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NcmStUfs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NcmId = table.Column<long>(type: "bigint", nullable: false),
                    UfOrigem = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    UfDestino = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Mva = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MvaAjustado = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AliquotaIcmsSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ReducaoBaseCalculoSt = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Cest = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    VigenciaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VigenciaFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NcmStUfs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NcmStUfs_Ncms_NcmId",
                        column: x => x.NcmId,
                        principalTable: "Ncms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NcmFederais_Codigo",
                table: "NcmFederais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NcmFederais_NcmId",
                table: "NcmFederais",
                column: "NcmId");

            migrationBuilder.CreateIndex(
                name: "IX_NcmIcmsUfs_Codigo",
                table: "NcmIcmsUfs",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NcmIcmsUfs_NcmId_Uf",
                table: "NcmIcmsUfs",
                columns: new[] { "NcmId", "Uf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ncms_Codigo",
                table: "Ncms",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ncms_CodigoNcm",
                table: "Ncms",
                column: "CodigoNcm",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NcmStUfs_Codigo",
                table: "NcmStUfs",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NcmStUfs_NcmId_UfOrigem_UfDestino",
                table: "NcmStUfs",
                columns: new[] { "NcmId", "UfOrigem", "UfDestino" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NcmFederais");

            migrationBuilder.DropTable(
                name: "NcmIcmsUfs");

            migrationBuilder.DropTable(
                name: "NcmStUfs");

            migrationBuilder.DropTable(
                name: "Ncms");
        }
    }
}
