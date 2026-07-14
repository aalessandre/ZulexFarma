using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase0EixoNoOrigemFilialDono : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Vouchers",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Vendas",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "VendaReceitas",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "VendaReceitaItens",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "VendaItemFiscais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "VendaFiscais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "VendaFarmaciaPopularItens",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "VendaFarmaciaPopulares",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ValoresAtributo",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "UsuariosGruposPermissao",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "UsuariosGrupos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Usuarios",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "UsuarioFilialGrupos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "TiposPagamento",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "SyncFila",
                newName: "NoOrigemId");

            migrationBuilder.RenameIndex(
                name: "IX_SyncFila_FilialOrigemId",
                table: "SyncFila",
                newName: "IX_SyncFila_NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Substancias",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "SubGrupos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "SngpcMapas",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "SequenciasCentrais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "SelfCheckoutTerminais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "SelfCheckoutConfiguracoes",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "SelfCheckoutConciliacoesEstoque",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "SelfCheckoutChamadosAtendente",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Secoes",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Promocoes",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosVariacoesValores",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosVariacoes",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosSubstancias",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosMs",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosLotes",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosLocais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosFornecedores",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosFiscal",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosDados",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosBarras",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutosAtributos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Produtos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ProdutoFamilias",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Prescritores",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "PremiosFidelidade",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "PlanosContas",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "PessoasEndereco",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "PessoasContato",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Pessoas",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "NcmStUfs",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Ncms",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "NcmIcmsUfs",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "NcmFederais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "NaturezasOperacao",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "NaturezaOperacaoRegras",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Municipios",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "MovimentosLote",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "MovimentosEstoque",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "MovimentosContaBancaria",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "LogsErro",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "LogsAcao",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "InventariosSngpcItens",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "InventariosSngpc",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "IcmsUfs",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "HierarquiasComissao",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "HierarquiaDescontos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "GruposProdutos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "GruposPrincipais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "GestorTributarioUsoMensais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "GestorTributarioJobs",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Fornecedores",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Filiais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Feriados",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Fabricantes",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Entregas",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "EntregaPerfis",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "EntregaFaixas",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "EntregaEventos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "EntregaAgendas",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Convenios",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ContasReceber",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ContasPagar",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ContasBancarias",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Configuracoes",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ComprasProdutosLotes",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ComprasProdutos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ComprasFiscal",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Compras",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ComissaoFaixasDesconto",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Colaboradores",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "ColaboradorComissaoAgrupadores",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Clientes",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "CertificadosDigitais",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "CampanhasFidelidadeItens",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "CampanhasFidelidade",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Caixas",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "CaixaMovimentos",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "CaixaFechamentoDeclarados",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "AtualizacoesPrecoItens",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "AtualizacoesPreco",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "AtributosVariacao",
                newName: "NoOrigemId");

            migrationBuilder.RenameColumn(
                name: "FilialOrigemId",
                table: "Adquirentes",
                newName: "NoOrigemId");

            migrationBuilder.AddColumn<long>(
                name: "FilialDonoId",
                table: "SyncFila",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncFila_FilialDonoId",
                table: "SyncFila",
                column: "FilialDonoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncFila_FilialDonoId",
                table: "SyncFila");

            migrationBuilder.DropColumn(
                name: "FilialDonoId",
                table: "SyncFila");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Vouchers",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Vendas",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "VendaReceitas",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "VendaReceitaItens",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "VendaItemFiscais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "VendaFiscais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "VendaFarmaciaPopularItens",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "VendaFarmaciaPopulares",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ValoresAtributo",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "UsuariosGruposPermissao",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "UsuariosGrupos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Usuarios",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "UsuarioFilialGrupos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "TiposPagamento",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "SyncFila",
                newName: "FilialOrigemId");

            migrationBuilder.RenameIndex(
                name: "IX_SyncFila_NoOrigemId",
                table: "SyncFila",
                newName: "IX_SyncFila_FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Substancias",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "SubGrupos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "SngpcMapas",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "SequenciasCentrais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "SelfCheckoutTerminais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "SelfCheckoutConfiguracoes",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "SelfCheckoutConciliacoesEstoque",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "SelfCheckoutChamadosAtendente",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Secoes",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Promocoes",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosVariacoesValores",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosVariacoes",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosSubstancias",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosMs",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosLotes",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosLocais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosFornecedores",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosFiscal",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosDados",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosBarras",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutosAtributos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Produtos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ProdutoFamilias",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Prescritores",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "PremiosFidelidade",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "PlanosContas",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "PessoasEndereco",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "PessoasContato",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Pessoas",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "NcmStUfs",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Ncms",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "NcmIcmsUfs",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "NcmFederais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "NaturezasOperacao",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "NaturezaOperacaoRegras",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Municipios",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "MovimentosLote",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "MovimentosEstoque",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "MovimentosContaBancaria",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "LogsErro",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "LogsAcao",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "InventariosSngpcItens",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "InventariosSngpc",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "IcmsUfs",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "HierarquiasComissao",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "HierarquiaDescontos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "GruposProdutos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "GruposPrincipais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "GestorTributarioUsoMensais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "GestorTributarioJobs",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Fornecedores",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Filiais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Feriados",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Fabricantes",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Entregas",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "EntregaPerfis",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "EntregaFaixas",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "EntregaEventos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "EntregaAgendas",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Convenios",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ContasReceber",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ContasPagar",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ContasBancarias",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Configuracoes",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ComprasProdutosLotes",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ComprasProdutos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ComprasFiscal",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Compras",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ComissaoFaixasDesconto",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Colaboradores",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "ColaboradorComissaoAgrupadores",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Clientes",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "CertificadosDigitais",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "CampanhasFidelidadeItens",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "CampanhasFidelidade",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Caixas",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "CaixaMovimentos",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "CaixaFechamentoDeclarados",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "AtualizacoesPrecoItens",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "AtualizacoesPreco",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "AtributosVariacao",
                newName: "FilialOrigemId");

            migrationBuilder.RenameColumn(
                name: "NoOrigemId",
                table: "Adquirentes",
                newName: "FilialOrigemId");
        }
    }
}
