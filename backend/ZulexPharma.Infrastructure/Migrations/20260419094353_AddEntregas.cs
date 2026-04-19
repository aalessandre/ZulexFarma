using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntregas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CoordenadasManual",
                table: "PessoasEndereco",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "PessoasEndereco",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "PessoasEndereco",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Filiais",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Filiais",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntregaFaixas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    RaioMaxKm = table.Column<decimal>(type: "numeric(8,3)", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregaFaixas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregaFaixas_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Entregas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendaId = table.Column<long>(type: "bigint", nullable: false),
                    FilialId = table.Column<long>(type: "bigint", nullable: false),
                    ClienteId = table.Column<long>(type: "bigint", nullable: false),
                    EnderecoEntregaId = table.Column<long>(type: "bigint", nullable: true),
                    Cep = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Rua = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Numero = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CodigoIbgeMunicipio = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", nullable: false),
                    EntregadorId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ValorEntrega = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DistanciaKm = table.Column<decimal>(type: "numeric(8,3)", nullable: false),
                    EntregaFaixaId = table.Column<long>(type: "bigint", nullable: true),
                    TokenRastreamento = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenEntregador = table.Column<Guid>(type: "uuid", nullable: false),
                    DataPedido = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataPrevista = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataSaida = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DataEntrega = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    FilialOrigemId = table.Column<long>(type: "bigint", nullable: true),
                    SyncGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entregas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entregas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entregas_Colaboradores_EntregadorId",
                        column: x => x.EntregadorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Entregas_EntregaFaixas_EntregaFaixaId",
                        column: x => x.EntregaFaixaId,
                        principalTable: "EntregaFaixas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Entregas_Filiais_FilialId",
                        column: x => x.FilialId,
                        principalTable: "Filiais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entregas_PessoasEndereco_EnderecoEntregaId",
                        column: x => x.EnderecoEntregaId,
                        principalTable: "PessoasEndereco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Entregas_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregaEventos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntregaId = table.Column<long>(type: "bigint", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    Texto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_EntregaEventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregaEventos_Entregas_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entregas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntregaEventos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntregaEventos_Codigo",
                table: "EntregaEventos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaEventos_EntregaId_CriadoEm",
                table: "EntregaEventos",
                columns: new[] { "EntregaId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_EntregaEventos_SyncGuid",
                table: "EntregaEventos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaEventos_UsuarioId",
                table: "EntregaEventos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFaixas_Codigo",
                table: "EntregaFaixas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFaixas_FilialId_RaioMaxKm",
                table: "EntregaFaixas",
                columns: new[] { "FilialId", "RaioMaxKm" });

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFaixas_SyncGuid",
                table: "EntregaFaixas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_ClienteId",
                table: "Entregas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_Codigo",
                table: "Entregas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_EnderecoEntregaId",
                table: "Entregas",
                column: "EnderecoEntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_EntregadorId",
                table: "Entregas",
                column: "EntregadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_EntregaFaixaId",
                table: "Entregas",
                column: "EntregaFaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_FilialId_DataPedido",
                table: "Entregas",
                columns: new[] { "FilialId", "DataPedido" });

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_FilialId_Status",
                table: "Entregas",
                columns: new[] { "FilialId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_SyncGuid",
                table: "Entregas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_TokenEntregador",
                table: "Entregas",
                column: "TokenEntregador",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_TokenRastreamento",
                table: "Entregas",
                column: "TokenRastreamento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_VendaId",
                table: "Entregas",
                column: "VendaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntregaEventos");

            migrationBuilder.DropTable(
                name: "Entregas");

            migrationBuilder.DropTable(
                name: "EntregaFaixas");

            migrationBuilder.DropColumn(
                name: "CoordenadasManual",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Filiais");
        }
    }
}
