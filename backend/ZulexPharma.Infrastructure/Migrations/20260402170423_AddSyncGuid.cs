using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "UsuariosGruposPermissao",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "UsuariosGrupos",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Usuarios",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "UsuarioFilialGrupos",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Substancias",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "SubGrupos",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Secoes",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ProdutosSubstancias",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ProdutosMs",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ProdutosLocais",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ProdutosFornecedores",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ProdutosFiscal",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ProdutosDados",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ProdutosBarras",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Produtos",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "ProdutoFamilias",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "PessoasEndereco",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "PessoasContato",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Pessoas",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "NcmStUfs",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Ncms",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "NcmIcmsUfs",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "NcmFederais",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "LogsErro",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "LogsAcao",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "GruposProdutos",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "GruposPrincipais",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Fornecedores",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Filiais",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Fabricantes",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "SyncGuid",
                table: "Colaboradores",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosGruposPermissao_SyncGuid",
                table: "UsuariosGruposPermissao",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosGrupos_SyncGuid",
                table: "UsuariosGrupos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_SyncGuid",
                table: "Usuarios",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioFilialGrupos_SyncGuid",
                table: "UsuarioFilialGrupos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Substancias_SyncGuid",
                table: "Substancias",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SubGrupos_SyncGuid",
                table: "SubGrupos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Secoes_SyncGuid",
                table: "Secoes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosSubstancias_SyncGuid",
                table: "ProdutosSubstancias",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosMs_SyncGuid",
                table: "ProdutosMs",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLocais_SyncGuid",
                table: "ProdutosLocais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFornecedores_SyncGuid",
                table: "ProdutosFornecedores",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFiscal_SyncGuid",
                table: "ProdutosFiscal",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_SyncGuid",
                table: "ProdutosDados",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosBarras_SyncGuid",
                table: "ProdutosBarras",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_SyncGuid",
                table: "Produtos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoFamilias_SyncGuid",
                table: "ProdutoFamilias",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_PessoasEndereco_SyncGuid",
                table: "PessoasEndereco",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_PessoasContato_SyncGuid",
                table: "PessoasContato",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_SyncGuid",
                table: "Pessoas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_NcmStUfs_SyncGuid",
                table: "NcmStUfs",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Ncms_SyncGuid",
                table: "Ncms",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_NcmIcmsUfs_SyncGuid",
                table: "NcmIcmsUfs",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_NcmFederais_SyncGuid",
                table: "NcmFederais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_LogsErro_SyncGuid",
                table: "LogsErro",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAcao_SyncGuid",
                table: "LogsAcao",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_GruposProdutos_SyncGuid",
                table: "GruposProdutos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_GruposPrincipais_SyncGuid",
                table: "GruposPrincipais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_SyncGuid",
                table: "Fornecedores",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Filiais_SyncGuid",
                table: "Filiais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Fabricantes_SyncGuid",
                table: "Fabricantes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_SyncGuid",
                table: "Colaboradores",
                column: "SyncGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsuariosGruposPermissao_SyncGuid",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosGrupos_SyncGuid",
                table: "UsuariosGrupos");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_SyncGuid",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_UsuarioFilialGrupos_SyncGuid",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropIndex(
                name: "IX_Substancias_SyncGuid",
                table: "Substancias");

            migrationBuilder.DropIndex(
                name: "IX_SubGrupos_SyncGuid",
                table: "SubGrupos");

            migrationBuilder.DropIndex(
                name: "IX_Secoes_SyncGuid",
                table: "Secoes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosSubstancias_SyncGuid",
                table: "ProdutosSubstancias");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosMs_SyncGuid",
                table: "ProdutosMs");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosLocais_SyncGuid",
                table: "ProdutosLocais");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosFornecedores_SyncGuid",
                table: "ProdutosFornecedores");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosFiscal_SyncGuid",
                table: "ProdutosFiscal");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosDados_SyncGuid",
                table: "ProdutosDados");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosBarras_SyncGuid",
                table: "ProdutosBarras");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_SyncGuid",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_ProdutoFamilias_SyncGuid",
                table: "ProdutoFamilias");

            migrationBuilder.DropIndex(
                name: "IX_PessoasEndereco_SyncGuid",
                table: "PessoasEndereco");

            migrationBuilder.DropIndex(
                name: "IX_PessoasContato_SyncGuid",
                table: "PessoasContato");

            migrationBuilder.DropIndex(
                name: "IX_Pessoas_SyncGuid",
                table: "Pessoas");

            migrationBuilder.DropIndex(
                name: "IX_NcmStUfs_SyncGuid",
                table: "NcmStUfs");

            migrationBuilder.DropIndex(
                name: "IX_Ncms_SyncGuid",
                table: "Ncms");

            migrationBuilder.DropIndex(
                name: "IX_NcmIcmsUfs_SyncGuid",
                table: "NcmIcmsUfs");

            migrationBuilder.DropIndex(
                name: "IX_NcmFederais_SyncGuid",
                table: "NcmFederais");

            migrationBuilder.DropIndex(
                name: "IX_LogsErro_SyncGuid",
                table: "LogsErro");

            migrationBuilder.DropIndex(
                name: "IX_LogsAcao_SyncGuid",
                table: "LogsAcao");

            migrationBuilder.DropIndex(
                name: "IX_GruposProdutos_SyncGuid",
                table: "GruposProdutos");

            migrationBuilder.DropIndex(
                name: "IX_GruposPrincipais_SyncGuid",
                table: "GruposPrincipais");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_SyncGuid",
                table: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_Filiais_SyncGuid",
                table: "Filiais");

            migrationBuilder.DropIndex(
                name: "IX_Fabricantes_SyncGuid",
                table: "Fabricantes");

            migrationBuilder.DropIndex(
                name: "IX_Colaboradores_SyncGuid",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "UsuariosGrupos");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Substancias");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "SubGrupos");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Secoes");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ProdutosSubstancias");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ProdutosMs");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ProdutosLocais");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ProdutosFornecedores");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ProdutosBarras");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "ProdutoFamilias");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "PessoasContato");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "NcmStUfs");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Ncms");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "NcmIcmsUfs");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "NcmFederais");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "LogsErro");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "LogsAcao");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "GruposProdutos");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "GruposPrincipais");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Fabricantes");

            migrationBuilder.DropColumn(
                name: "SyncGuid",
                table: "Colaboradores");
        }
    }
}
