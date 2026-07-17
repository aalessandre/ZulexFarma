using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZulexPharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase4CodigoUnicoComposto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FASE 4b (achado CRITICO da revisao adversarial, CONFIRMADO no banco de dev): a
            // reciclagem de sequences do passado ja' criou (Codigo, NoOrigemId) DUPLICADOS em
            // Pessoas/PessoasEndereco/Fornecedores/Fabricantes — o CREATE UNIQUE INDEX abaixo
            // mataria o boot (fail-fast do MigrateAsync). Re-codifica as duplicatas MAIS NOVAS
            // (mantem a de menor Id) como '{Codigo}-r{Id}' ANTES de criar os indices.
            // Nota: correcao LOCAL (nao replica) — as duplicatas eram fruto de bug local.
            migrationBuilder.Sql("""
                DO $$
                DECLARE t record;
                BEGIN
                  FOR t IN
                    SELECT c.table_name FROM information_schema.columns c
                    WHERE c.column_name = 'Codigo' AND c.table_schema = 'public'
                      AND EXISTS (SELECT 1 FROM information_schema.columns c2
                                  WHERE c2.table_name = c.table_name
                                    AND c2.column_name = 'NoOrigemId' AND c2.table_schema = 'public')
                  LOOP
                    EXECUTE format(
                      'UPDATE %I x SET "Codigo" = x."Codigo" || ''-r'' || x."Id"
                       FROM (SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "Codigo", "NoOrigemId" ORDER BY "Id") rn
                             FROM %I WHERE "Codigo" IS NOT NULL AND "NoOrigemId" IS NOT NULL) d
                       WHERE x."Id" = d."Id" AND d.rn > 1', t.table_name, t.table_name);
                  END LOOP;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Codigo",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_Codigo",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitas_Codigo",
                table: "VendaReceitas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitaItens_Codigo",
                table: "VendaReceitaItens");

            migrationBuilder.DropIndex(
                name: "IX_VendaItemFiscais_Codigo",
                table: "VendaItemFiscais");

            migrationBuilder.DropIndex(
                name: "IX_VendaFiscais_Codigo",
                table: "VendaFiscais");

            migrationBuilder.DropIndex(
                name: "IX_VendaFarmaciaPopularItens_Codigo",
                table: "VendaFarmaciaPopularItens");

            migrationBuilder.DropIndex(
                name: "IX_VendaFarmaciaPopulares_Codigo",
                table: "VendaFarmaciaPopulares");

            migrationBuilder.DropIndex(
                name: "IX_ValoresAtributo_Codigo",
                table: "ValoresAtributo");

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
                name: "IX_TiposPagamento_Codigo",
                table: "TiposPagamento");

            migrationBuilder.DropIndex(
                name: "IX_Substancias_Codigo",
                table: "Substancias");

            migrationBuilder.DropIndex(
                name: "IX_SubGrupos_Codigo",
                table: "SubGrupos");

            migrationBuilder.DropIndex(
                name: "IX_SngpcMapas_Codigo",
                table: "SngpcMapas");

            migrationBuilder.DropIndex(
                name: "IX_SequenciasCentrais_Codigo",
                table: "SequenciasCentrais");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutTerminais_Codigo",
                table: "SelfCheckoutTerminais");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutConfiguracoes_Codigo",
                table: "SelfCheckoutConfiguracoes");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_Codigo",
                table: "SelfCheckoutConciliacoesEstoque");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutChamadosAtendente_Codigo",
                table: "SelfCheckoutChamadosAtendente");

            migrationBuilder.DropIndex(
                name: "IX_Secoes_Codigo",
                table: "Secoes");

            migrationBuilder.DropIndex(
                name: "IX_Promocoes_Codigo",
                table: "Promocoes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosVariacoesValores_Codigo",
                table: "ProdutosVariacoesValores");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosVariacoes_Codigo",
                table: "ProdutosVariacoes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosSubstancias_Codigo",
                table: "ProdutosSubstancias");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosMs_Codigo",
                table: "ProdutosMs");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosLotes_Codigo",
                table: "ProdutosLotes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosLocais_Codigo",
                table: "ProdutosLocais");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosFornecedores_Codigo",
                table: "ProdutosFornecedores");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosFiscal_Codigo",
                table: "ProdutosFiscal");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosDados_Codigo",
                table: "ProdutosDados");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosBarras_Codigo",
                table: "ProdutosBarras");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosAtributos_Codigo",
                table: "ProdutosAtributos");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_Codigo",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_ProdutoFamilias_Codigo",
                table: "ProdutoFamilias");

            migrationBuilder.DropIndex(
                name: "IX_Prescritores_Codigo",
                table: "Prescritores");

            migrationBuilder.DropIndex(
                name: "IX_PremiosFidelidade_Codigo",
                table: "PremiosFidelidade");

            migrationBuilder.DropIndex(
                name: "IX_PlanosContas_Codigo",
                table: "PlanosContas");

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
                name: "IX_NcmStUfs_Codigo",
                table: "NcmStUfs");

            migrationBuilder.DropIndex(
                name: "IX_Ncms_Codigo",
                table: "Ncms");

            migrationBuilder.DropIndex(
                name: "IX_NcmIcmsUfs_Codigo",
                table: "NcmIcmsUfs");

            migrationBuilder.DropIndex(
                name: "IX_NcmFederais_Codigo",
                table: "NcmFederais");

            migrationBuilder.DropIndex(
                name: "IX_NaturezasOperacao_Codigo",
                table: "NaturezasOperacao");

            migrationBuilder.DropIndex(
                name: "IX_NaturezaOperacaoRegras_Codigo",
                table: "NaturezaOperacaoRegras");

            migrationBuilder.DropIndex(
                name: "IX_Municipios_Codigo",
                table: "Municipios");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosLote_Codigo",
                table: "MovimentosLote");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosEstoque_Codigo",
                table: "MovimentosEstoque");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosContaBancaria_Codigo",
                table: "MovimentosContaBancaria");

            migrationBuilder.DropIndex(
                name: "IX_LogsErro_Codigo",
                table: "LogsErro");

            migrationBuilder.DropIndex(
                name: "IX_LogsAcao_Codigo",
                table: "LogsAcao");

            migrationBuilder.DropIndex(
                name: "IX_InventariosSngpcItens_Codigo",
                table: "InventariosSngpcItens");

            migrationBuilder.DropIndex(
                name: "IX_InventariosSngpc_Codigo",
                table: "InventariosSngpc");

            migrationBuilder.DropIndex(
                name: "IX_IcmsUfs_Codigo",
                table: "IcmsUfs");

            migrationBuilder.DropIndex(
                name: "IX_HierarquiasComissao_Codigo",
                table: "HierarquiasComissao");

            migrationBuilder.DropIndex(
                name: "IX_HierarquiaDescontos_Codigo",
                table: "HierarquiaDescontos");

            migrationBuilder.DropIndex(
                name: "IX_GruposProdutos_Codigo",
                table: "GruposProdutos");

            migrationBuilder.DropIndex(
                name: "IX_GruposPrincipais_Codigo",
                table: "GruposPrincipais");

            migrationBuilder.DropIndex(
                name: "IX_GestorTributarioUsoMensais_Codigo",
                table: "GestorTributarioUsoMensais");

            migrationBuilder.DropIndex(
                name: "IX_GestorTributarioJobs_Codigo",
                table: "GestorTributarioJobs");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_Codigo",
                table: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_Filiais_Codigo",
                table: "Filiais");

            migrationBuilder.DropIndex(
                name: "IX_Feriados_Codigo",
                table: "Feriados");

            migrationBuilder.DropIndex(
                name: "IX_Fabricantes_Codigo",
                table: "Fabricantes");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_Codigo",
                table: "Entregas");

            migrationBuilder.DropIndex(
                name: "IX_EntregaPerfis_Codigo",
                table: "EntregaPerfis");

            migrationBuilder.DropIndex(
                name: "IX_EntregaFaixas_Codigo",
                table: "EntregaFaixas");

            migrationBuilder.DropIndex(
                name: "IX_EntregaEventos_Codigo",
                table: "EntregaEventos");

            migrationBuilder.DropIndex(
                name: "IX_EntregaAgendas_Codigo",
                table: "EntregaAgendas");

            migrationBuilder.DropIndex(
                name: "IX_Convenios_Codigo",
                table: "Convenios");

            migrationBuilder.DropIndex(
                name: "IX_ContasReceber_Codigo",
                table: "ContasReceber");

            migrationBuilder.DropIndex(
                name: "IX_ContasPagar_Codigo",
                table: "ContasPagar");

            migrationBuilder.DropIndex(
                name: "IX_ContasBancarias_Codigo",
                table: "ContasBancarias");

            migrationBuilder.DropIndex(
                name: "IX_Configuracoes_Codigo",
                table: "Configuracoes");

            migrationBuilder.DropIndex(
                name: "IX_ComprasProdutosLotes_Codigo",
                table: "ComprasProdutosLotes");

            migrationBuilder.DropIndex(
                name: "IX_ComprasProdutos_Codigo",
                table: "ComprasProdutos");

            migrationBuilder.DropIndex(
                name: "IX_ComprasFiscal_Codigo",
                table: "ComprasFiscal");

            migrationBuilder.DropIndex(
                name: "IX_Compras_Codigo",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_ComissaoFaixasDesconto_Codigo",
                table: "ComissaoFaixasDesconto");

            migrationBuilder.DropIndex(
                name: "IX_Colaboradores_Codigo",
                table: "Colaboradores");

            migrationBuilder.DropIndex(
                name: "IX_ColaboradorComissaoAgrupadores_Codigo",
                table: "ColaboradorComissaoAgrupadores");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Codigo",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_CertificadosDigitais_Codigo",
                table: "CertificadosDigitais");

            migrationBuilder.DropIndex(
                name: "IX_CampanhasFidelidadeItens_Codigo",
                table: "CampanhasFidelidadeItens");

            migrationBuilder.DropIndex(
                name: "IX_CampanhasFidelidade_Codigo",
                table: "CampanhasFidelidade");

            migrationBuilder.DropIndex(
                name: "IX_Caixas_Codigo",
                table: "Caixas");

            migrationBuilder.DropIndex(
                name: "IX_CaixaMovimentos_Codigo",
                table: "CaixaMovimentos");

            migrationBuilder.DropIndex(
                name: "IX_CaixaFechamentoDeclarados_Codigo",
                table: "CaixaFechamentoDeclarados");

            migrationBuilder.DropIndex(
                name: "IX_AtualizacoesPrecoItens_Codigo",
                table: "AtualizacoesPrecoItens");

            migrationBuilder.DropIndex(
                name: "IX_AtualizacoesPreco_Codigo",
                table: "AtualizacoesPreco");

            migrationBuilder.DropIndex(
                name: "IX_AtributosVariacao_Codigo",
                table: "AtributosVariacao");

            migrationBuilder.DropIndex(
                name: "IX_Adquirentes_Codigo",
                table: "Adquirentes");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Codigo_NoOrigemId",
                table: "Vouchers",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Codigo_NoOrigemId",
                table: "Vendas",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitas_Codigo_NoOrigemId",
                table: "VendaReceitas",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_Codigo_NoOrigemId",
                table: "VendaReceitaItens",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItemFiscais_Codigo_NoOrigemId",
                table: "VendaItemFiscais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_Codigo_NoOrigemId",
                table: "VendaFiscais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopularItens_Codigo_NoOrigemId",
                table: "VendaFarmaciaPopularItens",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_Codigo_NoOrigemId",
                table: "VendaFarmaciaPopulares",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ValoresAtributo_Codigo_NoOrigemId",
                table: "ValoresAtributo",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosGruposPermissao_Codigo_NoOrigemId",
                table: "UsuariosGruposPermissao",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosGrupos_Codigo_NoOrigemId",
                table: "UsuariosGrupos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Codigo_NoOrigemId",
                table: "Usuarios",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioFilialGrupos_Codigo_NoOrigemId",
                table: "UsuarioFilialGrupos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TiposPagamento_Codigo_NoOrigemId",
                table: "TiposPagamento",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Substancias_Codigo_NoOrigemId",
                table: "Substancias",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubGrupos_Codigo_NoOrigemId",
                table: "SubGrupos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SngpcMapas_Codigo_NoOrigemId",
                table: "SngpcMapas",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SequenciasCentrais_Codigo_NoOrigemId",
                table: "SequenciasCentrais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutTerminais_Codigo_NoOrigemId",
                table: "SelfCheckoutTerminais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConfiguracoes_Codigo_NoOrigemId",
                table: "SelfCheckoutConfiguracoes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_Codigo_NoOrigemId",
                table: "SelfCheckoutConciliacoesEstoque",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutChamadosAtendente_Codigo_NoOrigemId",
                table: "SelfCheckoutChamadosAtendente",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Secoes_Codigo_NoOrigemId",
                table: "Secoes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Promocoes_Codigo_NoOrigemId",
                table: "Promocoes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoesValores_Codigo_NoOrigemId",
                table: "ProdutosVariacoesValores",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoes_Codigo_NoOrigemId",
                table: "ProdutosVariacoes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosSubstancias_Codigo_NoOrigemId",
                table: "ProdutosSubstancias",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosMs_Codigo_NoOrigemId",
                table: "ProdutosMs",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_Codigo_NoOrigemId",
                table: "ProdutosLotes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLocais_Codigo_NoOrigemId",
                table: "ProdutosLocais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFornecedores_Codigo_NoOrigemId",
                table: "ProdutosFornecedores",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFiscal_Codigo_NoOrigemId",
                table: "ProdutosFiscal",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_Codigo_NoOrigemId",
                table: "ProdutosDados",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosBarras_Codigo_NoOrigemId",
                table: "ProdutosBarras",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosAtributos_Codigo_NoOrigemId",
                table: "ProdutosAtributos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Codigo_NoOrigemId",
                table: "Produtos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoFamilias_Codigo_NoOrigemId",
                table: "ProdutoFamilias",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Prescritores_Codigo_NoOrigemId",
                table: "Prescritores",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PremiosFidelidade_Codigo_NoOrigemId",
                table: "PremiosFidelidade",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosContas_Codigo_NoOrigemId",
                table: "PlanosContas",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PessoasEndereco_Codigo_NoOrigemId",
                table: "PessoasEndereco",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PessoasContato_Codigo_NoOrigemId",
                table: "PessoasContato",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pessoas_Codigo_NoOrigemId",
                table: "Pessoas",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NcmStUfs_Codigo_NoOrigemId",
                table: "NcmStUfs",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ncms_Codigo_NoOrigemId",
                table: "Ncms",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NcmIcmsUfs_Codigo_NoOrigemId",
                table: "NcmIcmsUfs",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NcmFederais_Codigo_NoOrigemId",
                table: "NcmFederais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NaturezasOperacao_Codigo_NoOrigemId",
                table: "NaturezasOperacao",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NaturezaOperacaoRegras_Codigo_NoOrigemId",
                table: "NaturezaOperacaoRegras",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_Codigo_NoOrigemId",
                table: "Municipios",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_Codigo_NoOrigemId",
                table: "MovimentosLote",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_Codigo_NoOrigemId",
                table: "MovimentosEstoque",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_Codigo_NoOrigemId",
                table: "MovimentosContaBancaria",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogsErro_Codigo_NoOrigemId",
                table: "LogsErro",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAcao_Codigo_NoOrigemId",
                table: "LogsAcao",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpcItens_Codigo_NoOrigemId",
                table: "InventariosSngpcItens",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpc_Codigo_NoOrigemId",
                table: "InventariosSngpc",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IcmsUfs_Codigo_NoOrigemId",
                table: "IcmsUfs",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiasComissao_Codigo_NoOrigemId",
                table: "HierarquiasComissao",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontos_Codigo_NoOrigemId",
                table: "HierarquiaDescontos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GruposProdutos_Codigo_NoOrigemId",
                table: "GruposProdutos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GruposPrincipais_Codigo_NoOrigemId",
                table: "GruposPrincipais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioUsoMensais_Codigo_NoOrigemId",
                table: "GestorTributarioUsoMensais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioJobs_Codigo_NoOrigemId",
                table: "GestorTributarioJobs",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_Codigo_NoOrigemId",
                table: "Fornecedores",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Filiais_Codigo_NoOrigemId",
                table: "Filiais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_Codigo_NoOrigemId",
                table: "Feriados",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Fabricantes_Codigo_NoOrigemId",
                table: "Fabricantes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_Codigo_NoOrigemId",
                table: "Entregas",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaPerfis_Codigo_NoOrigemId",
                table: "EntregaPerfis",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFaixas_Codigo_NoOrigemId",
                table: "EntregaFaixas",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaEventos_Codigo_NoOrigemId",
                table: "EntregaEventos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaAgendas_Codigo_NoOrigemId",
                table: "EntregaAgendas",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Convenios_Codigo_NoOrigemId",
                table: "Convenios",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_Codigo_NoOrigemId",
                table: "ContasReceber",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_Codigo_NoOrigemId",
                table: "ContasPagar",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_Codigo_NoOrigemId",
                table: "ContasBancarias",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_Codigo_NoOrigemId",
                table: "Configuracoes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutosLotes_Codigo_NoOrigemId",
                table: "ComprasProdutosLotes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutos_Codigo_NoOrigemId",
                table: "ComprasProdutos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasFiscal_Codigo_NoOrigemId",
                table: "ComprasFiscal",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Codigo_NoOrigemId",
                table: "Compras",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComissaoFaixasDesconto_Codigo_NoOrigemId",
                table: "ComissaoFaixasDesconto",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_Codigo_NoOrigemId",
                table: "Colaboradores",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ColaboradorComissaoAgrupadores_Codigo_NoOrigemId",
                table: "ColaboradorComissaoAgrupadores",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Codigo_NoOrigemId",
                table: "Clientes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosDigitais_Codigo_NoOrigemId",
                table: "CertificadosDigitais",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_Codigo_NoOrigemId",
                table: "CampanhasFidelidadeItens",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidade_Codigo_NoOrigemId",
                table: "CampanhasFidelidade",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_Codigo_NoOrigemId",
                table: "Caixas",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_Codigo_NoOrigemId",
                table: "CaixaMovimentos",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaFechamentoDeclarados_Codigo_NoOrigemId",
                table: "CaixaFechamentoDeclarados",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPrecoItens_Codigo_NoOrigemId",
                table: "AtualizacoesPrecoItens",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPreco_Codigo_NoOrigemId",
                table: "AtualizacoesPreco",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AtributosVariacao_Codigo_NoOrigemId",
                table: "AtributosVariacao",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Adquirentes_Codigo_NoOrigemId",
                table: "Adquirentes",
                columns: new[] { "Codigo", "NoOrigemId" },
                unique: true,
                filter: "\"Codigo\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Codigo_NoOrigemId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vendas_Codigo_NoOrigemId",
                table: "Vendas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitas_Codigo_NoOrigemId",
                table: "VendaReceitas");

            migrationBuilder.DropIndex(
                name: "IX_VendaReceitaItens_Codigo_NoOrigemId",
                table: "VendaReceitaItens");

            migrationBuilder.DropIndex(
                name: "IX_VendaItemFiscais_Codigo_NoOrigemId",
                table: "VendaItemFiscais");

            migrationBuilder.DropIndex(
                name: "IX_VendaFiscais_Codigo_NoOrigemId",
                table: "VendaFiscais");

            migrationBuilder.DropIndex(
                name: "IX_VendaFarmaciaPopularItens_Codigo_NoOrigemId",
                table: "VendaFarmaciaPopularItens");

            migrationBuilder.DropIndex(
                name: "IX_VendaFarmaciaPopulares_Codigo_NoOrigemId",
                table: "VendaFarmaciaPopulares");

            migrationBuilder.DropIndex(
                name: "IX_ValoresAtributo_Codigo_NoOrigemId",
                table: "ValoresAtributo");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosGruposPermissao_Codigo_NoOrigemId",
                table: "UsuariosGruposPermissao");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosGrupos_Codigo_NoOrigemId",
                table: "UsuariosGrupos");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Codigo_NoOrigemId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_UsuarioFilialGrupos_Codigo_NoOrigemId",
                table: "UsuarioFilialGrupos");

            migrationBuilder.DropIndex(
                name: "IX_TiposPagamento_Codigo_NoOrigemId",
                table: "TiposPagamento");

            migrationBuilder.DropIndex(
                name: "IX_Substancias_Codigo_NoOrigemId",
                table: "Substancias");

            migrationBuilder.DropIndex(
                name: "IX_SubGrupos_Codigo_NoOrigemId",
                table: "SubGrupos");

            migrationBuilder.DropIndex(
                name: "IX_SngpcMapas_Codigo_NoOrigemId",
                table: "SngpcMapas");

            migrationBuilder.DropIndex(
                name: "IX_SequenciasCentrais_Codigo_NoOrigemId",
                table: "SequenciasCentrais");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutTerminais_Codigo_NoOrigemId",
                table: "SelfCheckoutTerminais");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutConfiguracoes_Codigo_NoOrigemId",
                table: "SelfCheckoutConfiguracoes");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_Codigo_NoOrigemId",
                table: "SelfCheckoutConciliacoesEstoque");

            migrationBuilder.DropIndex(
                name: "IX_SelfCheckoutChamadosAtendente_Codigo_NoOrigemId",
                table: "SelfCheckoutChamadosAtendente");

            migrationBuilder.DropIndex(
                name: "IX_Secoes_Codigo_NoOrigemId",
                table: "Secoes");

            migrationBuilder.DropIndex(
                name: "IX_Promocoes_Codigo_NoOrigemId",
                table: "Promocoes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosVariacoesValores_Codigo_NoOrigemId",
                table: "ProdutosVariacoesValores");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosVariacoes_Codigo_NoOrigemId",
                table: "ProdutosVariacoes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosSubstancias_Codigo_NoOrigemId",
                table: "ProdutosSubstancias");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosMs_Codigo_NoOrigemId",
                table: "ProdutosMs");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosLotes_Codigo_NoOrigemId",
                table: "ProdutosLotes");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosLocais_Codigo_NoOrigemId",
                table: "ProdutosLocais");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosFornecedores_Codigo_NoOrigemId",
                table: "ProdutosFornecedores");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosFiscal_Codigo_NoOrigemId",
                table: "ProdutosFiscal");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosDados_Codigo_NoOrigemId",
                table: "ProdutosDados");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosBarras_Codigo_NoOrigemId",
                table: "ProdutosBarras");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosAtributos_Codigo_NoOrigemId",
                table: "ProdutosAtributos");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_Codigo_NoOrigemId",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_ProdutoFamilias_Codigo_NoOrigemId",
                table: "ProdutoFamilias");

            migrationBuilder.DropIndex(
                name: "IX_Prescritores_Codigo_NoOrigemId",
                table: "Prescritores");

            migrationBuilder.DropIndex(
                name: "IX_PremiosFidelidade_Codigo_NoOrigemId",
                table: "PremiosFidelidade");

            migrationBuilder.DropIndex(
                name: "IX_PlanosContas_Codigo_NoOrigemId",
                table: "PlanosContas");

            migrationBuilder.DropIndex(
                name: "IX_PessoasEndereco_Codigo_NoOrigemId",
                table: "PessoasEndereco");

            migrationBuilder.DropIndex(
                name: "IX_PessoasContato_Codigo_NoOrigemId",
                table: "PessoasContato");

            migrationBuilder.DropIndex(
                name: "IX_Pessoas_Codigo_NoOrigemId",
                table: "Pessoas");

            migrationBuilder.DropIndex(
                name: "IX_NcmStUfs_Codigo_NoOrigemId",
                table: "NcmStUfs");

            migrationBuilder.DropIndex(
                name: "IX_Ncms_Codigo_NoOrigemId",
                table: "Ncms");

            migrationBuilder.DropIndex(
                name: "IX_NcmIcmsUfs_Codigo_NoOrigemId",
                table: "NcmIcmsUfs");

            migrationBuilder.DropIndex(
                name: "IX_NcmFederais_Codigo_NoOrigemId",
                table: "NcmFederais");

            migrationBuilder.DropIndex(
                name: "IX_NaturezasOperacao_Codigo_NoOrigemId",
                table: "NaturezasOperacao");

            migrationBuilder.DropIndex(
                name: "IX_NaturezaOperacaoRegras_Codigo_NoOrigemId",
                table: "NaturezaOperacaoRegras");

            migrationBuilder.DropIndex(
                name: "IX_Municipios_Codigo_NoOrigemId",
                table: "Municipios");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosLote_Codigo_NoOrigemId",
                table: "MovimentosLote");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosEstoque_Codigo_NoOrigemId",
                table: "MovimentosEstoque");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosContaBancaria_Codigo_NoOrigemId",
                table: "MovimentosContaBancaria");

            migrationBuilder.DropIndex(
                name: "IX_LogsErro_Codigo_NoOrigemId",
                table: "LogsErro");

            migrationBuilder.DropIndex(
                name: "IX_LogsAcao_Codigo_NoOrigemId",
                table: "LogsAcao");

            migrationBuilder.DropIndex(
                name: "IX_InventariosSngpcItens_Codigo_NoOrigemId",
                table: "InventariosSngpcItens");

            migrationBuilder.DropIndex(
                name: "IX_InventariosSngpc_Codigo_NoOrigemId",
                table: "InventariosSngpc");

            migrationBuilder.DropIndex(
                name: "IX_IcmsUfs_Codigo_NoOrigemId",
                table: "IcmsUfs");

            migrationBuilder.DropIndex(
                name: "IX_HierarquiasComissao_Codigo_NoOrigemId",
                table: "HierarquiasComissao");

            migrationBuilder.DropIndex(
                name: "IX_HierarquiaDescontos_Codigo_NoOrigemId",
                table: "HierarquiaDescontos");

            migrationBuilder.DropIndex(
                name: "IX_GruposProdutos_Codigo_NoOrigemId",
                table: "GruposProdutos");

            migrationBuilder.DropIndex(
                name: "IX_GruposPrincipais_Codigo_NoOrigemId",
                table: "GruposPrincipais");

            migrationBuilder.DropIndex(
                name: "IX_GestorTributarioUsoMensais_Codigo_NoOrigemId",
                table: "GestorTributarioUsoMensais");

            migrationBuilder.DropIndex(
                name: "IX_GestorTributarioJobs_Codigo_NoOrigemId",
                table: "GestorTributarioJobs");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_Codigo_NoOrigemId",
                table: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_Filiais_Codigo_NoOrigemId",
                table: "Filiais");

            migrationBuilder.DropIndex(
                name: "IX_Feriados_Codigo_NoOrigemId",
                table: "Feriados");

            migrationBuilder.DropIndex(
                name: "IX_Fabricantes_Codigo_NoOrigemId",
                table: "Fabricantes");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_Codigo_NoOrigemId",
                table: "Entregas");

            migrationBuilder.DropIndex(
                name: "IX_EntregaPerfis_Codigo_NoOrigemId",
                table: "EntregaPerfis");

            migrationBuilder.DropIndex(
                name: "IX_EntregaFaixas_Codigo_NoOrigemId",
                table: "EntregaFaixas");

            migrationBuilder.DropIndex(
                name: "IX_EntregaEventos_Codigo_NoOrigemId",
                table: "EntregaEventos");

            migrationBuilder.DropIndex(
                name: "IX_EntregaAgendas_Codigo_NoOrigemId",
                table: "EntregaAgendas");

            migrationBuilder.DropIndex(
                name: "IX_Convenios_Codigo_NoOrigemId",
                table: "Convenios");

            migrationBuilder.DropIndex(
                name: "IX_ContasReceber_Codigo_NoOrigemId",
                table: "ContasReceber");

            migrationBuilder.DropIndex(
                name: "IX_ContasPagar_Codigo_NoOrigemId",
                table: "ContasPagar");

            migrationBuilder.DropIndex(
                name: "IX_ContasBancarias_Codigo_NoOrigemId",
                table: "ContasBancarias");

            migrationBuilder.DropIndex(
                name: "IX_Configuracoes_Codigo_NoOrigemId",
                table: "Configuracoes");

            migrationBuilder.DropIndex(
                name: "IX_ComprasProdutosLotes_Codigo_NoOrigemId",
                table: "ComprasProdutosLotes");

            migrationBuilder.DropIndex(
                name: "IX_ComprasProdutos_Codigo_NoOrigemId",
                table: "ComprasProdutos");

            migrationBuilder.DropIndex(
                name: "IX_ComprasFiscal_Codigo_NoOrigemId",
                table: "ComprasFiscal");

            migrationBuilder.DropIndex(
                name: "IX_Compras_Codigo_NoOrigemId",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_ComissaoFaixasDesconto_Codigo_NoOrigemId",
                table: "ComissaoFaixasDesconto");

            migrationBuilder.DropIndex(
                name: "IX_Colaboradores_Codigo_NoOrigemId",
                table: "Colaboradores");

            migrationBuilder.DropIndex(
                name: "IX_ColaboradorComissaoAgrupadores_Codigo_NoOrigemId",
                table: "ColaboradorComissaoAgrupadores");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Codigo_NoOrigemId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_CertificadosDigitais_Codigo_NoOrigemId",
                table: "CertificadosDigitais");

            migrationBuilder.DropIndex(
                name: "IX_CampanhasFidelidadeItens_Codigo_NoOrigemId",
                table: "CampanhasFidelidadeItens");

            migrationBuilder.DropIndex(
                name: "IX_CampanhasFidelidade_Codigo_NoOrigemId",
                table: "CampanhasFidelidade");

            migrationBuilder.DropIndex(
                name: "IX_Caixas_Codigo_NoOrigemId",
                table: "Caixas");

            migrationBuilder.DropIndex(
                name: "IX_CaixaMovimentos_Codigo_NoOrigemId",
                table: "CaixaMovimentos");

            migrationBuilder.DropIndex(
                name: "IX_CaixaFechamentoDeclarados_Codigo_NoOrigemId",
                table: "CaixaFechamentoDeclarados");

            migrationBuilder.DropIndex(
                name: "IX_AtualizacoesPrecoItens_Codigo_NoOrigemId",
                table: "AtualizacoesPrecoItens");

            migrationBuilder.DropIndex(
                name: "IX_AtualizacoesPreco_Codigo_NoOrigemId",
                table: "AtualizacoesPreco");

            migrationBuilder.DropIndex(
                name: "IX_AtributosVariacao_Codigo_NoOrigemId",
                table: "AtributosVariacao");

            migrationBuilder.DropIndex(
                name: "IX_Adquirentes_Codigo_NoOrigemId",
                table: "Adquirentes");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Codigo",
                table: "Vouchers",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Codigo",
                table: "Vendas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitas_Codigo",
                table: "VendaReceitas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaReceitaItens_Codigo",
                table: "VendaReceitaItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItemFiscais_Codigo",
                table: "VendaItemFiscais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFiscais_Codigo",
                table: "VendaFiscais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopularItens_Codigo",
                table: "VendaFarmaciaPopularItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VendaFarmaciaPopulares_Codigo",
                table: "VendaFarmaciaPopulares",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ValoresAtributo_Codigo",
                table: "ValoresAtributo",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

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
                name: "IX_TiposPagamento_Codigo",
                table: "TiposPagamento",
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
                name: "IX_SngpcMapas_Codigo",
                table: "SngpcMapas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SequenciasCentrais_Codigo",
                table: "SequenciasCentrais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutTerminais_Codigo",
                table: "SelfCheckoutTerminais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConfiguracoes_Codigo",
                table: "SelfCheckoutConfiguracoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutConciliacoesEstoque_Codigo",
                table: "SelfCheckoutConciliacoesEstoque",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckoutChamadosAtendente_Codigo",
                table: "SelfCheckoutChamadosAtendente",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Secoes_Codigo",
                table: "Secoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Promocoes_Codigo",
                table: "Promocoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoesValores_Codigo",
                table: "ProdutosVariacoesValores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosVariacoes_Codigo",
                table: "ProdutosVariacoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosSubstancias_Codigo",
                table: "ProdutosSubstancias",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosMs_Codigo",
                table: "ProdutosMs",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLotes_Codigo",
                table: "ProdutosLotes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosLocais_Codigo",
                table: "ProdutosLocais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFornecedores_Codigo",
                table: "ProdutosFornecedores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosFiscal_Codigo",
                table: "ProdutosFiscal",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosDados_Codigo",
                table: "ProdutosDados",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosBarras_Codigo",
                table: "ProdutosBarras",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosAtributos_Codigo",
                table: "ProdutosAtributos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Codigo",
                table: "Produtos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoFamilias_Codigo",
                table: "ProdutoFamilias",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Prescritores_Codigo",
                table: "Prescritores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PremiosFidelidade_Codigo",
                table: "PremiosFidelidade",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosContas_Codigo",
                table: "PlanosContas",
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
                name: "IX_NcmStUfs_Codigo",
                table: "NcmStUfs",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Ncms_Codigo",
                table: "Ncms",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NcmIcmsUfs_Codigo",
                table: "NcmIcmsUfs",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NcmFederais_Codigo",
                table: "NcmFederais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NaturezasOperacao_Codigo",
                table: "NaturezasOperacao",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NaturezaOperacaoRegras_Codigo",
                table: "NaturezaOperacaoRegras",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_Codigo",
                table: "Municipios",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosLote_Codigo",
                table: "MovimentosLote",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_Codigo",
                table: "MovimentosEstoque",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosContaBancaria_Codigo",
                table: "MovimentosContaBancaria",
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
                name: "IX_InventariosSngpcItens_Codigo",
                table: "InventariosSngpcItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventariosSngpc_Codigo",
                table: "InventariosSngpc",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IcmsUfs_Codigo",
                table: "IcmsUfs",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiasComissao_Codigo",
                table: "HierarquiasComissao",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HierarquiaDescontos_Codigo",
                table: "HierarquiaDescontos",
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
                name: "IX_GestorTributarioUsoMensais_Codigo",
                table: "GestorTributarioUsoMensais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GestorTributarioJobs_Codigo",
                table: "GestorTributarioJobs",
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
                name: "IX_Feriados_Codigo",
                table: "Feriados",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Fabricantes_Codigo",
                table: "Fabricantes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_Codigo",
                table: "Entregas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaPerfis_Codigo",
                table: "EntregaPerfis",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaFaixas_Codigo",
                table: "EntregaFaixas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaEventos_Codigo",
                table: "EntregaEventos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaAgendas_Codigo",
                table: "EntregaAgendas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Convenios_Codigo",
                table: "Convenios",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_Codigo",
                table: "ContasReceber",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_Codigo",
                table: "ContasPagar",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBancarias_Codigo",
                table: "ContasBancarias",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_Codigo",
                table: "Configuracoes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutosLotes_Codigo",
                table: "ComprasProdutosLotes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasProdutos_Codigo",
                table: "ComprasProdutos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasFiscal_Codigo",
                table: "ComprasFiscal",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Codigo",
                table: "Compras",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComissaoFaixasDesconto_Codigo",
                table: "ComissaoFaixasDesconto",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_Codigo",
                table: "Colaboradores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ColaboradorComissaoAgrupadores_Codigo",
                table: "ColaboradorComissaoAgrupadores",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Codigo",
                table: "Clientes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CertificadosDigitais_Codigo",
                table: "CertificadosDigitais",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidadeItens_Codigo",
                table: "CampanhasFidelidadeItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampanhasFidelidade_Codigo",
                table: "CampanhasFidelidade",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_Codigo",
                table: "Caixas",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaMovimentos_Codigo",
                table: "CaixaMovimentos",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CaixaFechamentoDeclarados_Codigo",
                table: "CaixaFechamentoDeclarados",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPrecoItens_Codigo",
                table: "AtualizacoesPrecoItens",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AtualizacoesPreco_Codigo",
                table: "AtualizacoesPreco",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AtributosVariacao_Codigo",
                table: "AtributosVariacao",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Adquirentes_Codigo",
                table: "Adquirentes",
                column: "Codigo",
                filter: "\"Codigo\" IS NOT NULL");
        }
    }
}
