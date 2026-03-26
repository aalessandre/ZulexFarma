using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameGrupoTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GruposPermissao_GruposUsuario_GrupoUsuarioId",
                table: "GruposPermissao");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioFilialGrupos_GruposUsuario_GrupoUsuarioId",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_GruposUsuario_GrupoUsuarioId",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GruposUsuario",
                table: "GruposUsuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GruposPermissao",
                table: "GruposPermissao");

            migrationBuilder.RenameTable(
                name: "GruposUsuario",
                newName: "UsuariosGrupos");

            migrationBuilder.RenameTable(
                name: "GruposPermissao",
                newName: "UsuariosGruposPermissao");

            migrationBuilder.RenameIndex(
                name: "IX_GruposPermissao_GrupoUsuarioId",
                table: "UsuariosGruposPermissao",
                newName: "IX_UsuariosGruposPermissao_GrupoUsuarioId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuariosGrupos",
                table: "UsuariosGrupos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuariosGruposPermissao",
                table: "UsuariosGruposPermissao",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioFilialGrupos_UsuariosGrupos_GrupoUsuarioId",
                table: "UsuarioFilialGrupos",
                column: "GrupoUsuarioId",
                principalTable: "UsuariosGrupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_UsuariosGrupos_GrupoUsuarioId",
                table: "Usuarios",
                column: "GrupoUsuarioId",
                principalTable: "UsuariosGrupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosGruposPermissao_UsuariosGrupos_GrupoUsuarioId",
                table: "UsuariosGruposPermissao",
                column: "GrupoUsuarioId",
                principalTable: "UsuariosGrupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioFilialGrupos_UsuariosGrupos_GrupoUsuarioId",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_UsuariosGrupos_GrupoUsuarioId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosGruposPermissao_UsuariosGrupos_GrupoUsuarioId",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuariosGruposPermissao",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuariosGrupos",
                table: "UsuariosGrupos");

            migrationBuilder.RenameTable(
                name: "UsuariosGruposPermissao",
                newName: "GruposPermissao");

            migrationBuilder.RenameTable(
                name: "UsuariosGrupos",
                newName: "GruposUsuario");

            migrationBuilder.RenameIndex(
                name: "IX_UsuariosGruposPermissao_GrupoUsuarioId",
                table: "GruposPermissao",
                newName: "IX_GruposPermissao_GrupoUsuarioId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GruposPermissao",
                table: "GruposPermissao",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GruposUsuario",
                table: "GruposUsuario",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GruposPermissao_GruposUsuario_GrupoUsuarioId",
                table: "GruposPermissao",
                column: "GrupoUsuarioId",
                principalTable: "GruposUsuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioFilialGrupos_GruposUsuario_GrupoUsuarioId",
                table: "UsuarioFilialGrupos",
                column: "GrupoUsuarioId",
                principalTable: "GruposUsuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_GruposUsuario_GrupoUsuarioId",
                table: "Usuarios",
                column: "GrupoUsuarioId",
                principalTable: "GruposUsuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
