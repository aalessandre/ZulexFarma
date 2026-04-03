using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIcmsUfAndFilialAliquota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AliquotaIcms",
                table: "Filiais",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "IcmsUfs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    NomeEstado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AliquotaInterna = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IcmsUfs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IcmsUfs_Codigo",
                table: "IcmsUfs",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IcmsUfs_SyncGuid",
                table: "IcmsUfs",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_IcmsUfs_Uf",
                table: "IcmsUfs",
                column: "Uf",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IcmsUfs");

            migrationBuilder.DropColumn(
                name: "AliquotaIcms",
                table: "Filiais");
        }
    }
}
