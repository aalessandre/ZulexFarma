using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntregasPerfilAgendaFeriados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntregaFaixas_Filiais_FilialId",
                table: "EntregaFaixas");

            migrationBuilder.DropIndex(
                name: "IX_EntregaFaixas_FilialId_RaioMaxKm",
                table: "EntregaFaixas");

            migrationBuilder.RenameColumn(
                name: "FilialId",
                table: "EntregaFaixas",
                newName: "PerfilId");

            migrationBuilder.CreateTable(
                name: "EntregaPerfis",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregaPerfis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregaPerfis_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feriados",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Ambito = table.Column<int>(type: "integer", nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    FilialId = table.Column<long>(type: "bigint", nullable: true),
                    Origem = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feriados_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregaAgendas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: true),
                    Turno = table.Column<int>(type: "integer", nullable: false),
                    EhFeriado = table.Column<bool>(type: "boolean", nullable: false),
                    PerfilId = table.Column<long>(type: "bigint", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregaAgendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregaAgendas_EntregaPerfis_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "EntregaPerfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntregaAgendas_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ─────────────────────────────────────────────────────────────
            //  MIGRAÇÃO DE DADOS — tem que rodar ANTES do índice unique abaixo,
            //  porque as faixas pré-existentes podem ter combinações PerfilId(=FilialId
            //  antigo)+RaioMaxKm duplicadas. Após criar os perfis "PADRÃO", cada filial
            //  tem um perfil só, então a unicidade PerfilId+Raio fica garantida.
            // ─────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                -- 1) Criar perfil PADRÃO para cada filial que JÁ TEM faixas.
                INSERT INTO ""EntregaPerfis"" (""FilialId"", ""Nome"", ""Ativo"", ""CriadoEm"")
                SELECT DISTINCT f.""PerfilId"" AS ""FilialId"", 'PADRÃO', TRUE, (now() AT TIME ZONE 'UTC')
                FROM ""EntregaFaixas"" f
                WHERE f.""PerfilId"" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM ""EntregaPerfis"" p
                      WHERE p.""FilialId"" = f.""PerfilId"" AND p.""Nome"" = 'PADRÃO'
                  );

                -- 2) Atualizar PerfilId das faixas para apontar pro PADRÃO da filial.
                --    (antes do RenameColumn, PerfilId armazenava temporariamente o FilialId)
                UPDATE ""EntregaFaixas"" f
                SET ""PerfilId"" = p.""Id""
                FROM ""EntregaPerfis"" p
                WHERE p.""FilialId"" = f.""PerfilId"" AND p.""Nome"" = 'PADRÃO';

                -- 2.1) Dedup defensivo: se houver faixas com mesmo Raio dentro do mesmo perfil, manter só uma.
                DELETE FROM ""EntregaFaixas""
                WHERE ""Id"" IN (
                    SELECT ""Id"" FROM (
                        SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""PerfilId"", ""RaioMaxKm"" ORDER BY ""Id"") rn
                        FROM ""EntregaFaixas""
                    ) t WHERE rn > 1
                );

                -- 3) Garantir perfil PADRÃO para TODAS as filiais (mesmo sem faixas).
                INSERT INTO ""EntregaPerfis"" (""FilialId"", ""Nome"", ""Ativo"", ""CriadoEm"")
                SELECT f.""Id"", 'PADRÃO', TRUE, (now() AT TIME ZONE 'UTC')
                FROM ""Filiais"" f
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""EntregaPerfis"" p
                    WHERE p.""FilialId"" = f.""Id"" AND p.""Nome"" = 'PADRÃO'
                );

                -- 4) Criar 16 linhas de agenda por filial, todas apontando pro PADRÃO.
                --    14 slots normais (DiaSemana 1-7 × Turno 1-2) + 2 slots de feriado.
                INSERT INTO ""EntregaAgendas"" (""FilialId"", ""DiaSemana"", ""Turno"", ""EhFeriado"", ""PerfilId"", ""Ativo"", ""CriadoEm"")
                SELECT p.""FilialId"", d.dia, t.turno, FALSE, p.""Id"", TRUE, (now() AT TIME ZONE 'UTC')
                FROM ""EntregaPerfis"" p
                CROSS JOIN (VALUES (1),(2),(3),(4),(5),(6),(7)) AS d(dia)
                CROSS JOIN (VALUES (1),(2)) AS t(turno)
                WHERE p.""Nome"" = 'PADRÃO';

                -- 5) 2 slots de feriado por filial (DiaSemana NULL).
                INSERT INTO ""EntregaAgendas"" (""FilialId"", ""DiaSemana"", ""Turno"", ""EhFeriado"", ""PerfilId"", ""Ativo"", ""CriadoEm"")
                SELECT p.""FilialId"", NULL, t.turno, TRUE, p.""Id"", TRUE, (now() AT TIME ZONE 'UTC')
                FROM ""EntregaPerfis"" p
                CROSS JOIN (VALUES (1),(2)) AS t(turno)
                WHERE p.""Nome"" = 'PADRÃO';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFaixas_PerfilId_RaioMaxKm",
                table: "EntregaFaixas",
                columns: new[] { "PerfilId", "RaioMaxKm" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregaAgendas_Codigo",
                table: "EntregaAgendas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaAgendas_FilialId_DiaSemana_Turno_EhFeriado",
                table: "EntregaAgendas",
                columns: new[] { "FilialId", "DiaSemana", "Turno", "EhFeriado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregaAgendas_PerfilId",
                table: "EntregaAgendas",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaAgendas_SyncGuid",
                table: "EntregaAgendas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaPerfis_Codigo",
                table: "EntregaPerfis",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaPerfis_FilialId_Nome",
                table: "EntregaPerfis",
                columns: new[] { "FilialId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregaPerfis_SyncGuid",
                table: "EntregaPerfis",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_Codigo",
                table: "Feriados",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_Data",
                table: "Feriados",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_Data_Ambito_Uf_FilialId",
                table: "Feriados",
                columns: new[] { "Data", "Ambito", "Uf", "FilialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_FilialId",
                table: "Feriados",
                column: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_SyncGuid",
                table: "Feriados",
                column: "SyncGuid");

            migrationBuilder.AddForeignKey(
                name: "FK_EntregaFaixas_EntregaPerfis_PerfilId",
                table: "EntregaFaixas",
                column: "PerfilId",
                principalTable: "EntregaPerfis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntregaFaixas_EntregaPerfis_PerfilId",
                table: "EntregaFaixas");

            migrationBuilder.DropTable(
                name: "EntregaAgendas");

            migrationBuilder.DropTable(
                name: "Feriados");

            migrationBuilder.DropTable(
                name: "EntregaPerfis");

            migrationBuilder.DropIndex(
                name: "IX_EntregaFaixas_PerfilId_RaioMaxKm",
                table: "EntregaFaixas");

            migrationBuilder.RenameColumn(
                name: "PerfilId",
                table: "EntregaFaixas",
                newName: "FilialId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFaixas_FilialId_RaioMaxKm",
                table: "EntregaFaixas",
                columns: new[] { "FilialId", "RaioMaxKm" });

            migrationBuilder.AddForeignKey(
                name: "FK_EntregaFaixas_Filiais_FilialId",
                table: "EntregaFaixas",
                column: "FilialId",
                principalTable: "Filiais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
