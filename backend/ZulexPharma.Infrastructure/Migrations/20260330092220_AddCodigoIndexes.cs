using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UsuariosGruposPermissao_Codigo",
                table: "UsuariosGruposPermissao",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosGrupos_Codigo",
                table: "UsuariosGrupos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Codigo",
                table: "Usuarios",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioFilialGrupos_Codigo",
                table: "UsuarioFilialGrupos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Substancias_Codigo",
                table: "Substancias",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubGrupos_Codigo",
                table: "SubGrupos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Secoes_Codigo",
                table: "Secoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PessoasEndereco_Codigo",
                table: "PessoasEndereco",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PessoasContato_Codigo",
                table: "PessoasContato",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_Codigo",
                table: "Pessoas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogsErro_Codigo",
                table: "LogsErro",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAcao_Codigo",
                table: "LogsAcao",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GruposProdutos_Codigo",
                table: "GruposProdutos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GruposPrincipais_Codigo",
                table: "GruposPrincipais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_Codigo",
                table: "Fornecedores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Filiais_Codigo",
                table: "Filiais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Fabricantes_Codigo",
                table: "Fabricantes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_Codigo",
                table: "Colaboradores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsuariosGruposPermissao_Codigo",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosGrupos_Codigo",
                table: "UsuariosGrupos");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Codigo",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_UsuarioFilialGrupos_Codigo",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropIndex(
                name: "IX_Substancias_Codigo",
                table: "Substancias");

            migrationBuilder.DropIndex(
                name: "IX_SubGrupos_Codigo",
                table: "SubGrupos");

            migrationBuilder.DropIndex(
                name: "IX_Secoes_Codigo",
                table: "Secoes");

            migrationBuilder.DropIndex(
                name: "IX_PessoasEndereco_Codigo",
                table: "PessoasEndereco");

            migrationBuilder.DropIndex(
                name: "IX_PessoasContato_Codigo",
                table: "PessoasContato");

            migrationBuilder.DropIndex(
                name: "IX_Pessoas_Codigo",
                table: "Pessoas");

            migrationBuilder.DropIndex(
                name: "IX_LogsErro_Codigo",
                table: "LogsErro");

            migrationBuilder.DropIndex(
                name: "IX_LogsAcao_Codigo",
                table: "LogsAcao");

            migrationBuilder.DropIndex(
                name: "IX_GruposProdutos_Codigo",
                table: "GruposProdutos");

            migrationBuilder.DropIndex(
                name: "IX_GruposPrincipais_Codigo",
                table: "GruposPrincipais");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_Codigo",
                table: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_Filiais_Codigo",
                table: "Filiais");

            migrationBuilder.DropIndex(
                name: "IX_Fabricantes_Codigo",
                table: "Fabricantes");

            migrationBuilder.DropIndex(
                name: "IX_Colaboradores_Codigo",
                table: "Colaboradores");
        }
    }
}
