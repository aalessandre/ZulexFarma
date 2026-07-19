using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase3ConvergenciaLww : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_SyncGuid",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_SyncGuid",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitas_SyncGuid",
                table: "VendaReceitas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitaItens_SyncGuid",
                table: "VendaReceitaItens");

            migrationBuilder.DropIndex(
                name: "IX_VendaItemFiscais_SyncGuid",
                table: "VendaItemFiscais");

            migrationBuilder.DropIndex(
                name: "IX_VendaFiscais_SyncGuid",
                table: "VendaFiscais");

            migrationBuilder.DropIndex(
                name: "IX_VendaFarmaciaPopularItens_SyncGuid",
                table: "VendaFarmaciaPopularItens");

            migrationBuilder.DropIndex(
                name: "IX_VendaFarmaciaPopulares_SyncGuid",
                table: "VendaFarmaciaPopulares");

            migrationBuilder.DropIndex(
                name: "IX_ValoresAtributo_SyncGuid",
                table: "ValoresAtributo");

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
                name: "IX_TiposPagamento_SyncGuid",
                table: "TiposPagamento");

            migrationBuilder.DropIndex(
                name: "IX_Substancias_SyncGuid",
                table: "Substancias");

            migrationBuilder.DropIndex(
                name: "IX_SubGrupos_SyncGuid",
                table: "SubGrupos");

            migrationBuilder.DropIndex(
                name: "IX_SngpcMapas_SyncGuid",
                table: "SngpcMapas");

            migrationBuilder.DropIndex(
                name: "IX_SequenciasCentrais_SyncGuid",
                table: "SequenciasCentrais");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutTerminais_SyncGuid",
                table: "SelfCheckoutTerminais");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutConfiguracoes_SyncGuid",
                table: "SelfCheckoutConfiguracoes");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_SyncGuid",
                table: "SelfCheckoutConciliacoesEstoque");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutChamadosAtendente_SyncGuid",
                table: "SelfCheckoutChamadosAtendente");

            migrationBuilder.DropIndex(
                name: "IX_Secoes_SyncGuid",
                table: "Secoes");

            migrationBuilder.DropIndex(
                name: "IX_Promocoes_SyncGuid",
                table: "Promocoes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosVariacoesValores_SyncGuid",
                table: "ProdutosVariacoesValores");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosVariacoes_SyncGuid",
                table: "ProdutosVariacoes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosSubstancias_SyncGuid",
                table: "ProdutosSubstancias");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosMs_SyncGuid",
                table: "ProdutosMs");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosLotes_SyncGuid",
                table: "ProdutosLotes");

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
                name: "IX_ProdutosAtributos_SyncGuid",
                table: "ProdutosAtributos");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_SyncGuid",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_ProdutoFamilias_SyncGuid",
                table: "ProdutoFamilias");

            migrationBuilder.DropIndex(
                name: "IX_Prescritores_SyncGuid",
                table: "Prescritores");

            migrationBuilder.DropIndex(
                name: "IX_PremiosFidelidade_SyncGuid",
                table: "PremiosFidelidade");

            migrationBuilder.DropIndex(
                name: "IX_PlanosContas_SyncGuid",
                table: "PlanosContas");

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
                name: "IX_NaturezasOperacao_SyncGuid",
                table: "NaturezasOperacao");

            migrationBuilder.DropIndex(
                name: "IX_NaturezaOperacaoRegras_SyncGuid",
                table: "NaturezaOperacaoRegras");

            migrationBuilder.DropIndex(
                name: "IX_Municipios_SyncGuid",
                table: "Municipios");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosLote_SyncGuid",
                table: "MovimentosLote");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosEstoque_SyncGuid",
                table: "MovimentosEstoque");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosContaBancaria_SyncGuid",
                table: "MovimentosContaBancaria");

            migrationBuilder.DropIndex(
                name: "IX_LogsErro_SyncGuid",
                table: "LogsErro");

            migrationBuilder.DropIndex(
                name: "IX_LogsAcao_SyncGuid",
                table: "LogsAcao");

            migrationBuilder.DropIndex(
                name: "IX_InventariosSngpcItens_SyncGuid",
                table: "InventariosSngpcItens");

            migrationBuilder.DropIndex(
                name: "IX_InventariosSngpc_SyncGuid",
                table: "InventariosSngpc");

            migrationBuilder.DropIndex(
                name: "IX_IcmsUfs_SyncGuid",
                table: "IcmsUfs");

            migrationBuilder.DropIndex(
                name: "IX_HierarquiasComissao_SyncGuid",
                table: "HierarquiasComissao");

            migrationBuilder.DropIndex(
                name: "IX_HierarquiaDescontos_SyncGuid",
                table: "HierarquiaDescontos");

            migrationBuilder.DropIndex(
                name: "IX_GruposProdutos_SyncGuid",
                table: "GruposProdutos");

            migrationBuilder.DropIndex(
                name: "IX_GruposPrincipais_SyncGuid",
                table: "GruposPrincipais");

            migrationBuilder.DropIndex(
                name: "IX_GestorTributarioUsoMensais_SyncGuid",
                table: "GestorTributarioUsoMensais");

            migrationBuilder.DropIndex(
                name: "IX_GestorTributarioJobs_SyncGuid",
                table: "GestorTributarioJobs");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_SyncGuid",
                table: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_Filiais_SyncGuid",
                table: "Filiais");

            migrationBuilder.DropIndex(
                name: "IX_Feriados_SyncGuid",
                table: "Feriados");

            migrationBuilder.DropIndex(
                name: "IX_Fabricantes_SyncGuid",
                table: "Fabricantes");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_SyncGuid",
                table: "Entregas");

            migrationBuilder.DropIndex(
                name: "IX_EntregaPerfis_SyncGuid",
                table: "EntregaPerfis");

            migrationBuilder.DropIndex(
                name: "IX_EntregaFaixas_SyncGuid",
                table: "EntregaFaixas");

            migrationBuilder.DropIndex(
                name: "IX_EntregaEventos_SyncGuid",
                table: "EntregaEventos");

            migrationBuilder.DropIndex(
                name: "IX_EntregaAgendas_SyncGuid",
                table: "EntregaAgendas");

            migrationBuilder.DropIndex(
                name: "IX_Convenios_SyncGuid",
                table: "Convenios");

            migrationBuilder.DropIndex(
                name: "IX_ContasReceber_SyncGuid",
                table: "ContasReceber");

            migrationBuilder.DropIndex(
                name: "IX_ContasPagar_SyncGuid",
                table: "ContasPagar");

            migrationBuilder.DropIndex(
                name: "IX_ContasBancarias_SyncGuid",
                table: "ContasBancarias");

            migrationBuilder.DropIndex(
                name: "IX_Configuracoes_SyncGuid",
                table: "Configuracoes");

            migrationBuilder.DropIndex(
                name: "IX_ComprasProdutosLotes_SyncGuid",
                table: "ComprasProdutosLotes");

            migrationBuilder.DropIndex(
                name: "IX_ComprasProdutos_SyncGuid",
                table: "ComprasProdutos");

            migrationBuilder.DropIndex(
                name: "IX_ComprasFiscal_SyncGuid",
                table: "ComprasFiscal");

            migrationBuilder.DropIndex(
                name: "IX_Compras_SyncGuid",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_ComissaoFaixasDesconto_SyncGuid",
                table: "ComissaoFaixasDesconto");

            migrationBuilder.DropIndex(
                name: "IX_Colaboradores_SyncGuid",
                table: "Colaboradores");

            migrationBuilder.DropIndex(
                name: "IX_ColaboradorComissaoAgrupadores_SyncGuid",
                table: "ColaboradorComissaoAgrupadores");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_SyncGuid",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_CertificadosDigitais_SyncGuid",
                table: "CertificadosDigitais");

            migrationBuilder.DropIndex(
                name: "IX_CampanhasFidelidadeItens_SyncGuid",
                table: "CampanhasFidelidadeItens");

            migrationBuilder.DropIndex(
                name: "IX_CampanhasFidelidade_SyncGuid",
                table: "CampanhasFidelidade");

            migrationBuilder.DropIndex(
                name: "IX_Caixas_SyncGuid",
                table: "Caixas");

            migrationBuilder.DropIndex(
                name: "IX_CaixaMovimentos_SyncGuid",
                table: "CaixaMovimentos");

            migrationBuilder.DropIndex(
                name: "IX_CaixaFechamentoDeclarados_SyncGuid",
                table: "CaixaFechamentoDeclarados");

            migrationBuilder.DropIndex(
                name: "IX_AtualizacoesPrecoItens_SyncGuid",
                table: "AtualizacoesPrecoItens");

            migrationBuilder.DropIndex(
                name: "IX_AtualizacoesPreco_SyncGuid",
                table: "AtualizacoesPreco");

            migrationBuilder.DropIndex(
                name: "IX_AtributosVariacao_SyncGuid",
                table: "AtributosVariacao");

            migrationBuilder.DropIndex(
                name: "IX_Adquirentes_SyncGuid",
                table: "Adquirentes");

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Vouchers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Vendas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "VendaReceitas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "VendaReceitaItens",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "VendaItemFiscais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "VendaFiscais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "VendaFarmaciaPopularItens",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "VendaFarmaciaPopulares",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ValoresAtributo",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "UsuariosGruposPermissao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "UsuariosGrupos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Usuarios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "UsuarioFilialGrupos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "TiposPagamento",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Substancias",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "SubGrupos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "SngpcMapas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "SequenciasCentrais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "SelfCheckoutTerminais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "SelfCheckoutConfiguracoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "SelfCheckoutConciliacoesEstoque",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "SelfCheckoutChamadosAtendente",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Secoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Promocoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosVariacoesValores",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosVariacoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosSubstancias",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosMs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosLotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosLocais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosFornecedores",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosFiscal",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosDados",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosBarras",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutosAtributos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Produtos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ProdutoFamilias",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Prescritores",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "PremiosFidelidade",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "PlanosContas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "PessoasEndereco",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "PessoasContato",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Pessoas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "NcmStUfs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Ncms",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "NcmIcmsUfs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "NcmFederais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "NaturezasOperacao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "NaturezaOperacaoRegras",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Municipios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "MovimentosLote",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "MovimentosEstoque",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "MovimentosContaBancaria",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "LogsErro",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "LogsAcao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "InventariosSngpcItens",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "InventariosSngpc",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "IcmsUfs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "HierarquiasComissao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "HierarquiaDescontos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "GruposProdutos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "GruposPrincipais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "GestorTributarioUsoMensais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "GestorTributarioJobs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Fornecedores",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Filiais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Feriados",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Fabricantes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Entregas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "EntregaPerfis",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "EntregaFaixas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "EntregaEventos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "EntregaAgendas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Convenios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ContasReceber",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ContasPagar",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ContasBancarias",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Configuracoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ComprasProdutosLotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ComprasProdutos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ComprasFiscal",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Compras",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ComissaoFaixasDesconto",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Colaboradores",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "ColaboradorComissaoAgrupadores",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Clientes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "CertificadosDigitais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "CampanhasFidelidadeItens",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "CampanhasFidelidade",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Caixas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "CaixaMovimentos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "CaixaFechamentoDeclarados",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "AtualizacoesPrecoItens",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "AtualizacoesPreco",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "AtributosVariacao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AtualizadoPorNoId",
                table: "Adquirentes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_SyncGuid",
                table: "Vouchers",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_SyncGuid",
                table: "Vendas",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitas_SyncGuid",
                table: "VendaReceitas",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_SyncGuid",
                table: "VendaReceitaItens",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaItemFiscais_SyncGuid",
                table: "VendaItemFiscais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_SyncGuid",
                table: "VendaFiscais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopularItens_SyncGuid",
                table: "VendaFarmaciaPopularItens",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_SyncGuid",
                table: "VendaFarmaciaPopulares",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValoresAtributo_SyncGuid",
                table: "ValoresAtributo",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosGruposPermissao_SyncGuid",
                table: "UsuariosGruposPermissao",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosGrupos_SyncGuid",
                table: "UsuariosGrupos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_SyncGuid",
                table: "Usuarios",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioFilialGrupos_SyncGuid",
                table: "UsuarioFilialGrupos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposPagamento_SyncGuid",
                table: "TiposPagamento",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Substancias_SyncGuid",
                table: "Substancias",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubGrupos_SyncGuid",
                table: "SubGrupos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SngpcMapas_SyncGuid",
                table: "SngpcMapas",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SequenciasCentrais_SyncGuid",
                table: "SequenciasCentrais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutTerminais_SyncGuid",
                table: "SelfCheckoutTerminais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConfiguracoes_SyncGuid",
                table: "SelfCheckoutConfiguracoes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_SyncGuid",
                table: "SelfCheckoutConciliacoesEstoque",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutChamadosAtendente_SyncGuid",
                table: "SelfCheckoutChamadosAtendente",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Secoes_SyncGuid",
                table: "Secoes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promocoes_SyncGuid",
                table: "Promocoes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoesValores_SyncGuid",
                table: "ProdutosVariacoesValores",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoes_SyncGuid",
                table: "ProdutosVariacoes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosSubstancias_SyncGuid",
                table: "ProdutosSubstancias",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosMs_SyncGuid",
                table: "ProdutosMs",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_SyncGuid",
                table: "ProdutosLotes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLocais_SyncGuid",
                table: "ProdutosLocais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFornecedores_SyncGuid",
                table: "ProdutosFornecedores",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFiscal_SyncGuid",
                table: "ProdutosFiscal",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_SyncGuid",
                table: "ProdutosDados",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosBarras_SyncGuid",
                table: "ProdutosBarras",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosAtributos_SyncGuid",
                table: "ProdutosAtributos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_SyncGuid",
                table: "Produtos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoFamilias_SyncGuid",
                table: "ProdutoFamilias",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescritores_SyncGuid",
                table: "Prescritores",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PremiosFidelidade_SyncGuid",
                table: "PremiosFidelidade",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanosContas_SyncGuid",
                table: "PlanosContas",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PessoasEndereco_SyncGuid",
                table: "PessoasEndereco",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PessoasContato_SyncGuid",
                table: "PessoasContato",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_SyncGuid",
                table: "Pessoas",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NcmStUfs_SyncGuid",
                table: "NcmStUfs",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ncms_SyncGuid",
                table: "Ncms",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NcmIcmsUfs_SyncGuid",
                table: "NcmIcmsUfs",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NcmFederais_SyncGuid",
                table: "NcmFederais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NaturezasOperacao_SyncGuid",
                table: "NaturezasOperacao",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NaturezaOperacaoRegras_SyncGuid",
                table: "NaturezaOperacaoRegras",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_SyncGuid",
                table: "Municipios",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_SyncGuid",
                table: "MovimentosLote",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_SyncGuid",
                table: "MovimentosEstoque",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_SyncGuid",
                table: "MovimentosContaBancaria",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogsErro_SyncGuid",
                table: "LogsErro",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogsAcao_SyncGuid",
                table: "LogsAcao",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpcItens_SyncGuid",
                table: "InventariosSngpcItens",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpc_SyncGuid",
                table: "InventariosSngpc",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IcmsUfs_SyncGuid",
                table: "IcmsUfs",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiasComissao_SyncGuid",
                table: "HierarquiasComissao",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontos_SyncGuid",
                table: "HierarquiaDescontos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposProdutos_SyncGuid",
                table: "GruposProdutos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposPrincipais_SyncGuid",
                table: "GruposPrincipais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioUsoMensais_SyncGuid",
                table: "GestorTributarioUsoMensais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioJobs_SyncGuid",
                table: "GestorTributarioJobs",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_SyncGuid",
                table: "Fornecedores",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Filiais_SyncGuid",
                table: "Filiais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_SyncGuid",
                table: "Feriados",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fabricantes_SyncGuid",
                table: "Fabricantes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_SyncGuid",
                table: "Entregas",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregaPerfis_SyncGuid",
                table: "EntregaPerfis",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFaixas_SyncGuid",
                table: "EntregaFaixas",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregaEventos_SyncGuid",
                table: "EntregaEventos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregaAgendas_SyncGuid",
                table: "EntregaAgendas",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Convenios_SyncGuid",
                table: "Convenios",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_SyncGuid",
                table: "ContasReceber",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_SyncGuid",
                table: "ContasPagar",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_SyncGuid",
                table: "ContasBancarias",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_SyncGuid",
                table: "Configuracoes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutosLotes_SyncGuid",
                table: "ComprasProdutosLotes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutos_SyncGuid",
                table: "ComprasProdutos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprasFiscal_SyncGuid",
                table: "ComprasFiscal",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_SyncGuid",
                table: "Compras",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComissaoFaixasDesconto_SyncGuid",
                table: "ComissaoFaixasDesconto",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_SyncGuid",
                table: "Colaboradores",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ColaboradorComissaoAgrupadores_SyncGuid",
                table: "ColaboradorComissaoAgrupadores",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_SyncGuid",
                table: "Clientes",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosDigitais_SyncGuid",
                table: "CertificadosDigitais",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_SyncGuid",
                table: "CampanhasFidelidadeItens",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidade_SyncGuid",
                table: "CampanhasFidelidade",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_SyncGuid",
                table: "Caixas",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_SyncGuid",
                table: "CaixaMovimentos",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaixaFechamentoDeclarados_SyncGuid",
                table: "CaixaFechamentoDeclarados",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPrecoItens_SyncGuid",
                table: "AtualizacoesPrecoItens",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPreco_SyncGuid",
                table: "AtualizacoesPreco",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AtributosVariacao_SyncGuid",
                table: "AtributosVariacao",
                column: "SyncGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Adquirentes_SyncGuid",
                table: "Adquirentes",
                column: "SyncGuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_SyncGuid",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_SyncGuid",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitas_SyncGuid",
                table: "VendaReceitas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitaItens_SyncGuid",
                table: "VendaReceitaItens");

            migrationBuilder.DropIndex(
                name: "IX_VendaItemFiscais_SyncGuid",
                table: "VendaItemFiscais");

            migrationBuilder.DropIndex(
                name: "IX_VendaFiscais_SyncGuid",
                table: "VendaFiscais");

            migrationBuilder.DropIndex(
                name: "IX_VendaFarmaciaPopularItens_SyncGuid",
                table: "VendaFarmaciaPopularItens");

            migrationBuilder.DropIndex(
                name: "IX_VendaFarmaciaPopulares_SyncGuid",
                table: "VendaFarmaciaPopulares");

            migrationBuilder.DropIndex(
                name: "IX_ValoresAtributo_SyncGuid",
                table: "ValoresAtributo");

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
                name: "IX_TiposPagamento_SyncGuid",
                table: "TiposPagamento");

            migrationBuilder.DropIndex(
                name: "IX_Substancias_SyncGuid",
                table: "Substancias");

            migrationBuilder.DropIndex(
                name: "IX_SubGrupos_SyncGuid",
                table: "SubGrupos");

            migrationBuilder.DropIndex(
                name: "IX_SngpcMapas_SyncGuid",
                table: "SngpcMapas");

            migrationBuilder.DropIndex(
                name: "IX_SequenciasCentrais_SyncGuid",
                table: "SequenciasCentrais");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutTerminais_SyncGuid",
                table: "SelfCheckoutTerminais");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutConfiguracoes_SyncGuid",
                table: "SelfCheckoutConfiguracoes");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_SyncGuid",
                table: "SelfCheckoutConciliacoesEstoque");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutChamadosAtendente_SyncGuid",
                table: "SelfCheckoutChamadosAtendente");

            migrationBuilder.DropIndex(
                name: "IX_Secoes_SyncGuid",
                table: "Secoes");

            migrationBuilder.DropIndex(
                name: "IX_Promocoes_SyncGuid",
                table: "Promocoes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosVariacoesValores_SyncGuid",
                table: "ProdutosVariacoesValores");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosVariacoes_SyncGuid",
                table: "ProdutosVariacoes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosSubstancias_SyncGuid",
                table: "ProdutosSubstancias");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosMs_SyncGuid",
                table: "ProdutosMs");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosLotes_SyncGuid",
                table: "ProdutosLotes");

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
                name: "IX_ProdutosAtributos_SyncGuid",
                table: "ProdutosAtributos");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_SyncGuid",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_ProdutoFamilias_SyncGuid",
                table: "ProdutoFamilias");

            migrationBuilder.DropIndex(
                name: "IX_Prescritores_SyncGuid",
                table: "Prescritores");

            migrationBuilder.DropIndex(
                name: "IX_PremiosFidelidade_SyncGuid",
                table: "PremiosFidelidade");

            migrationBuilder.DropIndex(
                name: "IX_PlanosContas_SyncGuid",
                table: "PlanosContas");

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
                name: "IX_NaturezasOperacao_SyncGuid",
                table: "NaturezasOperacao");

            migrationBuilder.DropIndex(
                name: "IX_NaturezaOperacaoRegras_SyncGuid",
                table: "NaturezaOperacaoRegras");

            migrationBuilder.DropIndex(
                name: "IX_Municipios_SyncGuid",
                table: "Municipios");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosLote_SyncGuid",
                table: "MovimentosLote");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosEstoque_SyncGuid",
                table: "MovimentosEstoque");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosContaBancaria_SyncGuid",
                table: "MovimentosContaBancaria");

            migrationBuilder.DropIndex(
                name: "IX_LogsErro_SyncGuid",
                table: "LogsErro");

            migrationBuilder.DropIndex(
                name: "IX_LogsAcao_SyncGuid",
                table: "LogsAcao");

            migrationBuilder.DropIndex(
                name: "IX_InventariosSngpcItens_SyncGuid",
                table: "InventariosSngpcItens");

            migrationBuilder.DropIndex(
                name: "IX_InventariosSngpc_SyncGuid",
                table: "InventariosSngpc");

            migrationBuilder.DropIndex(
                name: "IX_IcmsUfs_SyncGuid",
                table: "IcmsUfs");

            migrationBuilder.DropIndex(
                name: "IX_HierarquiasComissao_SyncGuid",
                table: "HierarquiasComissao");

            migrationBuilder.DropIndex(
                name: "IX_HierarquiaDescontos_SyncGuid",
                table: "HierarquiaDescontos");

            migrationBuilder.DropIndex(
                name: "IX_GruposProdutos_SyncGuid",
                table: "GruposProdutos");

            migrationBuilder.DropIndex(
                name: "IX_GruposPrincipais_SyncGuid",
                table: "GruposPrincipais");

            migrationBuilder.DropIndex(
                name: "IX_GestorTributarioUsoMensais_SyncGuid",
                table: "GestorTributarioUsoMensais");

            migrationBuilder.DropIndex(
                name: "IX_GestorTributarioJobs_SyncGuid",
                table: "GestorTributarioJobs");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_SyncGuid",
                table: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_Filiais_SyncGuid",
                table: "Filiais");

            migrationBuilder.DropIndex(
                name: "IX_Feriados_SyncGuid",
                table: "Feriados");

            migrationBuilder.DropIndex(
                name: "IX_Fabricantes_SyncGuid",
                table: "Fabricantes");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_SyncGuid",
                table: "Entregas");

            migrationBuilder.DropIndex(
                name: "IX_EntregaPerfis_SyncGuid",
                table: "EntregaPerfis");

            migrationBuilder.DropIndex(
                name: "IX_EntregaFaixas_SyncGuid",
                table: "EntregaFaixas");

            migrationBuilder.DropIndex(
                name: "IX_EntregaEventos_SyncGuid",
                table: "EntregaEventos");

            migrationBuilder.DropIndex(
                name: "IX_EntregaAgendas_SyncGuid",
                table: "EntregaAgendas");

            migrationBuilder.DropIndex(
                name: "IX_Convenios_SyncGuid",
                table: "Convenios");

            migrationBuilder.DropIndex(
                name: "IX_ContasReceber_SyncGuid",
                table: "ContasReceber");

            migrationBuilder.DropIndex(
                name: "IX_ContasPagar_SyncGuid",
                table: "ContasPagar");

            migrationBuilder.DropIndex(
                name: "IX_ContasBancarias_SyncGuid",
                table: "ContasBancarias");

            migrationBuilder.DropIndex(
                name: "IX_Configuracoes_SyncGuid",
                table: "Configuracoes");

            migrationBuilder.DropIndex(
                name: "IX_ComprasProdutosLotes_SyncGuid",
                table: "ComprasProdutosLotes");

            migrationBuilder.DropIndex(
                name: "IX_ComprasProdutos_SyncGuid",
                table: "ComprasProdutos");

            migrationBuilder.DropIndex(
                name: "IX_ComprasFiscal_SyncGuid",
                table: "ComprasFiscal");

            migrationBuilder.DropIndex(
                name: "IX_Compras_SyncGuid",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_ComissaoFaixasDesconto_SyncGuid",
                table: "ComissaoFaixasDesconto");

            migrationBuilder.DropIndex(
                name: "IX_Colaboradores_SyncGuid",
                table: "Colaboradores");

            migrationBuilder.DropIndex(
                name: "IX_ColaboradorComissaoAgrupadores_SyncGuid",
                table: "ColaboradorComissaoAgrupadores");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_SyncGuid",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_CertificadosDigitais_SyncGuid",
                table: "CertificadosDigitais");

            migrationBuilder.DropIndex(
                name: "IX_CampanhasFidelidadeItens_SyncGuid",
                table: "CampanhasFidelidadeItens");

            migrationBuilder.DropIndex(
                name: "IX_CampanhasFidelidade_SyncGuid",
                table: "CampanhasFidelidade");

            migrationBuilder.DropIndex(
                name: "IX_Caixas_SyncGuid",
                table: "Caixas");

            migrationBuilder.DropIndex(
                name: "IX_CaixaMovimentos_SyncGuid",
                table: "CaixaMovimentos");

            migrationBuilder.DropIndex(
                name: "IX_CaixaFechamentoDeclarados_SyncGuid",
                table: "CaixaFechamentoDeclarados");

            migrationBuilder.DropIndex(
                name: "IX_AtualizacoesPrecoItens_SyncGuid",
                table: "AtualizacoesPrecoItens");

            migrationBuilder.DropIndex(
                name: "IX_AtualizacoesPreco_SyncGuid",
                table: "AtualizacoesPreco");

            migrationBuilder.DropIndex(
                name: "IX_AtributosVariacao_SyncGuid",
                table: "AtributosVariacao");

            migrationBuilder.DropIndex(
                name: "IX_Adquirentes_SyncGuid",
                table: "Adquirentes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "VendaReceitas");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "VendaReceitaItens");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "VendaItemFiscais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "VendaFiscais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "VendaFarmaciaPopularItens");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "VendaFarmaciaPopulares");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ValoresAtributo");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "UsuariosGrupos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "TiposPagamento");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Substancias");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "SubGrupos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "SngpcMapas");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "SequenciasCentrais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "SelfCheckoutTerminais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "SelfCheckoutConfiguracoes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "SelfCheckoutConciliacoesEstoque");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "SelfCheckoutChamadosAtendente");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Secoes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Promocoes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosVariacoesValores");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosVariacoes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosSubstancias");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosMs");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosLotes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosLocais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosFornecedores");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosFiscal");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosDados");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosBarras");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutosAtributos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ProdutoFamilias");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Prescritores");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "PremiosFidelidade");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "PlanosContas");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "PessoasEndereco");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "PessoasContato");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "NcmStUfs");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Ncms");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "NcmIcmsUfs");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "NcmFederais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "NaturezasOperacao");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "NaturezaOperacaoRegras");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Municipios");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "MovimentosLote");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "MovimentosEstoque");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "MovimentosContaBancaria");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "LogsErro");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "LogsAcao");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "InventariosSngpcItens");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "InventariosSngpc");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "IcmsUfs");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "HierarquiasComissao");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "HierarquiaDescontos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "GruposProdutos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "GruposPrincipais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "GestorTributarioUsoMensais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "GestorTributarioJobs");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Filiais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Feriados");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Fabricantes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Entregas");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "EntregaPerfis");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "EntregaFaixas");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "EntregaEventos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "EntregaAgendas");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Convenios");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ContasReceber");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ContasPagar");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ContasBancarias");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ComprasProdutosLotes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ComprasProdutos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ComprasFiscal");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ComissaoFaixasDesconto");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "ColaboradorComissaoAgrupadores");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "CertificadosDigitais");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "CampanhasFidelidadeItens");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "CampanhasFidelidade");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Caixas");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "CaixaMovimentos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "CaixaFechamentoDeclarados");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "AtualizacoesPrecoItens");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "AtualizacoesPreco");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "AtributosVariacao");

            migrationBuilder.DropColumn(
                name: "AtualizadoPorNoId",
                table: "Adquirentes");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_SyncGuid",
                table: "Vouchers",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_SyncGuid",
                table: "Vendas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitas_SyncGuid",
                table: "VendaReceitas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_SyncGuid",
                table: "VendaReceitaItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItemFiscais_SyncGuid",
                table: "VendaItemFiscais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_SyncGuid",
                table: "VendaFiscais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopularItens_SyncGuid",
                table: "VendaFarmaciaPopularItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_SyncGuid",
                table: "VendaFarmaciaPopulares",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ValoresAtributo_SyncGuid",
                table: "ValoresAtributo",
                column: "SyncGuid");

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
                name: "IX_TiposPagamento_SyncGuid",
                table: "TiposPagamento",
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
                name: "IX_SngpcMapas_SyncGuid",
                table: "SngpcMapas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SequenciasCentrais_SyncGuid",
                table: "SequenciasCentrais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutTerminais_SyncGuid",
                table: "SelfCheckoutTerminais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConfiguracoes_SyncGuid",
                table: "SelfCheckoutConfiguracoes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_SyncGuid",
                table: "SelfCheckoutConciliacoesEstoque",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutChamadosAtendente_SyncGuid",
                table: "SelfCheckoutChamadosAtendente",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Secoes_SyncGuid",
                table: "Secoes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Promocoes_SyncGuid",
                table: "Promocoes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoesValores_SyncGuid",
                table: "ProdutosVariacoesValores",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoes_SyncGuid",
                table: "ProdutosVariacoes",
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
                name: "IX_ProdutosLotes_SyncGuid",
                table: "ProdutosLotes",
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
                name: "IX_ProdutosAtributos_SyncGuid",
                table: "ProdutosAtributos",
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
                name: "IX_Prescritores_SyncGuid",
                table: "Prescritores",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_PremiosFidelidade_SyncGuid",
                table: "PremiosFidelidade",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosContas_SyncGuid",
                table: "PlanosContas",
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
                name: "IX_NaturezasOperacao_SyncGuid",
                table: "NaturezasOperacao",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_NaturezaOperacaoRegras_SyncGuid",
                table: "NaturezaOperacaoRegras",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_SyncGuid",
                table: "Municipios",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_SyncGuid",
                table: "MovimentosLote",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_SyncGuid",
                table: "MovimentosEstoque",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_SyncGuid",
                table: "MovimentosContaBancaria",
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
                name: "IX_InventariosSngpcItens_SyncGuid",
                table: "InventariosSngpcItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpc_SyncGuid",
                table: "InventariosSngpc",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_IcmsUfs_SyncGuid",
                table: "IcmsUfs",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiasComissao_SyncGuid",
                table: "HierarquiasComissao",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontos_SyncGuid",
                table: "HierarquiaDescontos",
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
                name: "IX_GestorTributarioUsoMensais_SyncGuid",
                table: "GestorTributarioUsoMensais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioJobs_SyncGuid",
                table: "GestorTributarioJobs",
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
                name: "IX_Feriados_SyncGuid",
                table: "Feriados",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Fabricantes_SyncGuid",
                table: "Fabricantes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_SyncGuid",
                table: "Entregas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaPerfis_SyncGuid",
                table: "EntregaPerfis",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFaixas_SyncGuid",
                table: "EntregaFaixas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaEventos_SyncGuid",
                table: "EntregaEventos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaAgendas_SyncGuid",
                table: "EntregaAgendas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Convenios_SyncGuid",
                table: "Convenios",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_SyncGuid",
                table: "ContasReceber",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_SyncGuid",
                table: "ContasPagar",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_SyncGuid",
                table: "ContasBancarias",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_SyncGuid",
                table: "Configuracoes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutosLotes_SyncGuid",
                table: "ComprasProdutosLotes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutos_SyncGuid",
                table: "ComprasProdutos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasFiscal_SyncGuid",
                table: "ComprasFiscal",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_SyncGuid",
                table: "Compras",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ComissaoFaixasDesconto_SyncGuid",
                table: "ComissaoFaixasDesconto",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_SyncGuid",
                table: "Colaboradores",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ColaboradorComissaoAgrupadores_SyncGuid",
                table: "ColaboradorComissaoAgrupadores",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_SyncGuid",
                table: "Clientes",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosDigitais_SyncGuid",
                table: "CertificadosDigitais",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_SyncGuid",
                table: "CampanhasFidelidadeItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidade_SyncGuid",
                table: "CampanhasFidelidade",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_SyncGuid",
                table: "Caixas",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_SyncGuid",
                table: "CaixaMovimentos",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaFechamentoDeclarados_SyncGuid",
                table: "CaixaFechamentoDeclarados",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPrecoItens_SyncGuid",
                table: "AtualizacoesPrecoItens",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPreco_SyncGuid",
                table: "AtualizacoesPreco",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_AtributosVariacao_SyncGuid",
                table: "AtributosVariacao",
                column: "SyncGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Adquirentes_SyncGuid",
                table: "Adquirentes",
                column: "SyncGuid");
        }
    }
}
