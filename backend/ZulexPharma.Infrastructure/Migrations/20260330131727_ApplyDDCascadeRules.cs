using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplyDDCascadeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colaboradores_Pessoas_PessoaId",
                table: "Colaboradores");

            migrationBuilder.DropForeignKey(
                name: "FK_Fornecedores_Pessoas_PessoaId",
                table: "Fornecedores");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioFilialGrupos_Usuarios_UsuarioId",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Colaboradores_ColaboradorId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosGruposPermissao_UsuariosGrupos_GrupoUsuarioId",
                table: "UsuariosGruposPermissao");

            migrationBuilder.AddForeignKey(
                name: "FK_Colaboradores_Pessoas_PessoaId",
                table: "Colaboradores",
                column: "PessoaId",
                principalTable: "Pessoas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Fornecedores_Pessoas_PessoaId",
                table: "Fornecedores",
                column: "PessoaId",
                principalTable: "Pessoas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioFilialGrupos_Usuarios_UsuarioId",
                table: "UsuarioFilialGrupos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Colaboradores_ColaboradorId",
                table: "Usuarios",
                column: "ColaboradorId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosGruposPermissao_UsuariosGrupos_GrupoUsuarioId",
                table: "UsuariosGruposPermissao",
                column: "GrupoUsuarioId",
                principalTable: "UsuariosGrupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colaboradores_Pessoas_PessoaId",
                table: "Colaboradores");

            migrationBuilder.DropForeignKey(
                name: "FK_Fornecedores_Pessoas_PessoaId",
                table: "Fornecedores");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioFilialGrupos_Usuarios_UsuarioId",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Colaboradores_ColaboradorId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosGruposPermissao_UsuariosGrupos_GrupoUsuarioId",
                table: "UsuariosGruposPermissao");

            migrationBuilder.AddForeignKey(
                name: "FK_Colaboradores_Pessoas_PessoaId",
                table: "Colaboradores",
                column: "PessoaId",
                principalTable: "Pessoas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fornecedores_Pessoas_PessoaId",
                table: "Fornecedores",
                column: "PessoaId",
                principalTable: "Pessoas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioFilialGrupos_Usuarios_UsuarioId",
                table: "UsuarioFilialGrupos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Colaboradores_ColaboradorId",
                table: "Usuarios",
                column: "ColaboradorId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosGruposPermissao_UsuariosGrupos_GrupoUsuarioId",
                table: "UsuariosGruposPermissao",
                column: "GrupoUsuarioId",
                principalTable: "UsuariosGrupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
