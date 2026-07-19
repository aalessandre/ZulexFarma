using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Entities.SelfCheckout;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// FASE 4 — REGISTRY UNICO de replicacao (cura P0.8, fail-open de escopo).
/// TODA entidade do modelo tem classificacao EXPLICITA aqui; o boot valida contra o modelo EF e
/// FALHA ALTO se alguma ficar de fora (fail-closed: antes, entidade nova virava GLOBAL por omissao
/// — vazamento; e o dicionario do applicator ja' teve furo real de 31 tabelas que nao replicavam
/// em silencio, ex. 'ContaPagar replica, ContaReceber nao').
/// Fonte da classificacao: ContextDocuments/classificacao-replicacao.md (decisoes do dono, 2026-07-14).
/// </summary>
public enum EscopoSync
{
    /// <summary>Replica pra TODOS os nos (cadastros compartilhados).</summary>
    Global,
    /// <summary>Replica so' pro no da filial dona + hub (operacional).</summary>
    PorFilial,
    /// <summary>NUNCA replica (estado local, contador fiscal, credencial, fila).</summary>
    Infra
}

public static class SyncRegistry
{
    // ── INFRA (BaseEntity que NAO replica) ──────────────────────────────────
    // SequenciaCentral = contador fiscal pinado ao no dono (replicar sob LWW = numero duplicado).
    // Configuracao = decisao A4 (o cursor morava nela e replicava). CertificadoDigital = credencial.
    public static readonly HashSet<Type> InfraTipos = new()
    {
        typeof(Configuracao), typeof(CertificadoDigital), typeof(SequenciaCentral),
        typeof(GestorTributarioJob), typeof(GestorTributarioUsoMensal)
    };

    /// <summary>Tabelas (BaseEntity ou nao) fora do outbox/Codigo — consumida pela captura.</summary>
    public static readonly HashSet<string> TabelasInfra = new()
    {
        "SyncFila", "SequenciasLocais", "AbcFarmaBase", "CertificadosDigitais", "SefazNotas",
        "SequenciasCentrais", "GestorTributarioJobs", "GestorTributarioUsoMensais", "Configuracoes"
    };

    // ── POR-FILIAL direta (tem FilialId proprio) ───────────────────────────
    public static readonly HashSet<Type> PorFilialDireta = new()
    {
        typeof(ProdutoDados), typeof(ProdutoFiscal), typeof(ProdutoFornecedor), typeof(ProdutoLote),
        typeof(MovimentoEstoque), typeof(AtualizacaoPreco),
        typeof(Venda), typeof(VendaReceita), typeof(Caixa),
        typeof(Compra), typeof(ContaPagar), typeof(ContaReceber), typeof(ContaBancaria),
        typeof(Entrega), typeof(EntregaPerfil), typeof(EntregaAgenda),
        typeof(InventarioSngpc), typeof(SngpcMapa),
        typeof(SelfCheckoutTerminal), typeof(SelfCheckoutConfiguracao)
    };

    // ── POR-FILIAL derivada (BaseEntity filha: herda a filial do pai via mapa de derivacao) ─
    public static readonly HashSet<Type> PorFilialDerivada = new()
    {
        typeof(CaixaMovimento), typeof(CaixaFechamentoDeclarado), typeof(AtualizacaoPrecoItem),
        typeof(MovimentoLote), typeof(MovimentoContaBancaria),
        typeof(VendaFiscal), typeof(VendaItemFiscal), typeof(VendaReceitaItem),
        typeof(CompraProduto), typeof(CompraFiscal), typeof(CompraProdutoLote),
        typeof(EntregaFaixa), typeof(EntregaEvento),
        typeof(VendaFarmaciaPopular), typeof(VendaFarmaciaPopularItem), typeof(InventarioSngpcItem),
        typeof(SelfCheckoutChamadoAtendente), typeof(SelfCheckoutConciliacaoEstoque)
    };

    // ── GLOBAL (replica pra todos) — EXPLICITO: o que nao esta em nenhum conjunto QUEBRA O BOOT ─
    // LogAcao/LogErro: dono decidiu POR-FILIAL no futuro (backlog B10-adjacente); hoje seguem o
    // comportamento vigente (global) — reclassificar exige coluna FilialId + migracao.
    public static readonly HashSet<Type> Globais = new()
    {
        typeof(Adquirente), typeof(AtributoVariacao), typeof(CampanhaFidelidade), typeof(CampanhaFidelidadeItem),
        typeof(Cliente), typeof(Colaborador), typeof(ColaboradorComissaoAgrupador), typeof(ComissaoFaixaDesconto),
        typeof(Convenio), typeof(Fabricante), typeof(Feriado), typeof(Filial), typeof(Fornecedor),
        typeof(GrupoPermissao), typeof(GrupoUsuario), typeof(GrupoPrincipal), typeof(GrupoProduto),
        typeof(SubGrupo), typeof(Secao), typeof(HierarquiaComissao), typeof(HierarquiaDesconto),
        typeof(IcmsUf), typeof(LogAcao), typeof(LogErro), typeof(Municipio),
        typeof(NaturezaOperacao), typeof(NaturezaOperacaoRegra),
        typeof(Ncm), typeof(NcmFederal), typeof(NcmIcmsUf), typeof(NcmStUf),
        typeof(Pessoa), typeof(PessoaContato), typeof(PessoaEndereco), typeof(PlanoConta),
        typeof(PremioFidelidade), typeof(Prescritor), typeof(Produto), typeof(ProdutoAtributo),
        typeof(ProdutoBarras), typeof(ProdutoFamilia), typeof(ProdutoLocal), typeof(ProdutoMs),
        typeof(ProdutoSubstancia), typeof(ProdutoVariacao), typeof(ProdutoVariacaoValor),
        typeof(Promocao), typeof(Substancia), typeof(TipoPagamento), typeof(Usuario),
        typeof(UsuarioFilialGrupo), typeof(ValorAtributo), typeof(Voucher),
        // FASE 6 (b+c): as 5 folhas de Cliente promovidas a BaseEntity — replicam sozinhas (uniao).
        typeof(ClienteConvenio), typeof(ClienteAutorizacao), typeof(ClienteDesconto),
        typeof(ClienteUsoContinuo), typeof(ClienteBloqueio)
    };

    /// <summary>
    /// FASE 6 (b+c, invariante de boot) — colecoes de filho POCO de agregado GLOBAL cuja semantica de
    /// conflito e' SUBSTITUICAO (LWW-agregado) ACEITA e revisada por humano. Cada entrada e' a afirmacao
    /// "aqui a perda-como-LWW e' a semantica acordada; nao e' silenciosa, esta' declarada". Uma colecao
    /// POCO de pai Global FORA desta whitelist derruba o boot (ValidarModelo) — forca a decisao
    /// (promover a BaseEntity = uniao, OU declarar aqui = substituicao), nunca deixa virar perda muda.
    /// Inclui os netos POCO (Secao/Tarifa). Filhos de Venda (PorFilial) nao entram (nao sao Global).
    /// DECIDIDO pelo dono (18/07/2026): TODAS as 19 ficam SUBSTITUICAO — nenhuma vira uniao. Prova no
    /// codigo: os 6 pais (Convenio/Promocao/HierarquiaDesconto/HierarquiaComissao/Adquirente/
    /// CampanhaFidelidade) editam os filhos em BLOCO (RemoveRange + re-add no AtualizarAsync), entao
    /// LWW-agregado (o form inteiro mais novo vence) e' a semantica CERTA; uniao fundiria dois forms
    /// concorrentes num estado que nenhum editor autorou. A assimetria com os filhos de Cliente (que
    /// viraram uniao na fase 6) e' PROPOSITAL: Cliente e' multi-fluxo/alta-concorrencia (uma loja poe
    /// bloqueio no balcao sem tocar no resto), estes sao config central editada de uma vez so'. Revisar
    /// SO' se a operacao real mostrar dois nos adicionando vinculos INDEPENDENTES ao mesmo pai.
    /// </summary>
    public static readonly HashSet<Type> ColecoesPocoSubstituicaoAceitas = new()
    {
        typeof(ConvenioDesconto), typeof(ConvenioBloqueio),
        typeof(PromocaoFaixa), typeof(PromocaoFilial), typeof(PromocaoPagamento),
        typeof(PromocaoConvenio), typeof(PromocaoProduto),
        typeof(HierarquiaDescontoItem), typeof(HierarquiaDescontoSecao), typeof(HierarquiaDescontoColaborador),
        typeof(HierarquiaDescontoConvenio), typeof(HierarquiaDescontoCliente),
        typeof(HierarquiaComissaoItem), typeof(HierarquiaComissaoSecao), typeof(HierarquiaComissaoColaborador),
        typeof(AdquirenteBandeira), typeof(AdquirenteTarifa),
        typeof(CampanhaFidelidadeFilial), typeof(CampanhaFidelidadePagamento)
    };

    // ── POCOs de INFRA (sem FK de agregado — nao replicam nunca; o resto dos POCOs precisa ter FK) ─
    public static readonly HashSet<Type> PocosInfra = new()
    {
        typeof(SyncFila), typeof(SyncQuarentena), typeof(SyncTombstone), typeof(SyncEstadoLocal),
        typeof(SyncNo), typeof(SyncNoFilial), typeof(SequenciaLocal),
        typeof(DicionarioTabela), typeof(DicionarioRevisao), typeof(DicionarioRelacionamento),
        typeof(AbcFarmaBase), typeof(SefazNota), typeof(IbptTax), typeof(RamoVisibilidade)
    };

    // ── LEDGER (fatos imutaveis): applicator aceita SO' 'I' (dedup por Id); U/D = LedgerImutavel ─
    public static readonly HashSet<string> TabelasLedger = new() { "MovimentosEstoque", "MovimentosLote" };

    public static EscopoSync EscopoDe(Type tipo, string tabela)
    {
        if (TabelasInfra.Contains(tabela) || InfraTipos.Contains(tipo)) return EscopoSync.Infra;
        if (PorFilialDireta.Contains(tipo) || PorFilialDerivada.Contains(tipo)) return EscopoSync.PorFilial;
        return EscopoSync.Global; // so' alcancavel apos ValidarModelo garantir classificacao explicita
    }

    /// <summary>
    /// FAIL-CLOSED no boot: toda BaseEntity do modelo classificada em EXATAMENTE um escopo; toda
    /// replicavel presente no dicionario do applicator; PorFilial com FilialId ou derivacao; POCO
    /// com FK de agregado ou marcado infra. Qualquer furo = boot NAO sobe, com a lista nominal.
    /// </summary>
    public static void ValidarModelo(IModel model)
    {
        var problemas = new List<string>();
        foreach (var et in model.GetEntityTypes())
        {
            var clr = et.ClrType;
            if (typeof(BaseEntity).IsAssignableFrom(clr))
            {
                var quantos = (InfraTipos.Contains(clr) ? 1 : 0) + (PorFilialDireta.Contains(clr) ? 1 : 0)
                            + (PorFilialDerivada.Contains(clr) ? 1 : 0) + (Globais.Contains(clr) ? 1 : 0);
                if (quantos == 0)
                    problemas.Add($"{clr.Name}: BaseEntity SEM classificacao no SyncRegistry (Global/PorFilial/Infra) — classifique EXPLICITAMENTE");
                else if (quantos > 1)
                    problemas.Add($"{clr.Name}: classificada em MAIS de um escopo no SyncRegistry");

                var tabela = et.GetTableName();
                if (quantos == 1 && !InfraTipos.Contains(clr) && tabela != null
                    && !TabelasInfra.Contains(tabela) && SyncApplicator.ResolverTipo(tabela) == null)
                    problemas.Add($"{clr.Name}: replica mas falta no dicionario do applicator (ResolverTipo('{tabela}')) — a op cairia como TipoDesconhecido no destino");

                if (PorFilialDireta.Contains(clr) && et.FindProperty("FilialId") == null)
                    problemas.Add($"{clr.Name}: PorFilial DIRETA sem propriedade FilialId");
                if (PorFilialDerivada.Contains(clr) && !AppDbContext.DerivacaoFilialDono.ContainsKey(clr))
                    problemas.Add($"{clr.Name}: PorFilial DERIVADA sem entrada no mapa de derivacao (AppDbContext.DerivacaoFilialDono)");
            }
            else if (!PocosInfra.Contains(clr) && clr.Namespace?.StartsWith("ZulexPharma.Domain") == true)
            {
                if (!et.GetForeignKeys().Any())
                    problemas.Add($"{clr.Name}: POCO sem FK — nao pertence a agregado nem esta marcado em PocosInfra (replicaria como... nada, em silencio)");
            }
        }

        // FASE 6 (b+c, invariante de boot): nenhuma colecao de filho POCO de agregado GLOBAL pode
        // existir sem estar classificada — ou o filho e' BaseEntity (uniao, replica sozinho) ou esta'
        // em ColecoesPocoSubstituicaoAceitas (substituicao aceita, declarada e revisavel). Senao, sob
        // concorrencia, o delete-missing do agregado vencedor apagaria o filho do perdedor EM SILENCIO
        // (o ALTO da auditoria cross-cutting). Isto torna "nada silencioso" verdade ESTRUTURAL no boot.
        var visitadosPoco = new HashSet<Type>();
        foreach (var et in model.GetEntityTypes())
            if (Globais.Contains(et.ClrType))
                ColetarPocoNaoClassificado(et, problemas, visitadosPoco);

        if (problemas.Count > 0)
            throw new InvalidOperationException(
                "SyncRegistry INVALIDO (fail-closed do plano, fase 4). Corrija ANTES de subir:\n - " +
                string.Join("\n - ", problemas));
    }

    /// <summary>
    /// FASE 6 — varre as colecoes de filho POCO de um agregado (recursivo pelos POCO, pra pegar netos
    /// Secao/Tarifa). Cada colecao POCO fora da whitelist vira problema. Para nos filhos BaseEntity
    /// (replicam sozinhos) e memoiza os POCO visitados (anti-ciclo). Mesmo predicado de nav-colecao
    /// nao-BaseEntity do delete-missing real (SyncApplicator.ReconciliarColecoesAsync).
    /// </summary>
    private static void ColetarPocoNaoClassificado(IEntityType et, List<string> problemas, HashSet<Type> visitados)
    {
        foreach (var nav in et.GetNavigations())
        {
            if (!nav.IsCollection) continue;
            var alvo = nav.TargetEntityType.ClrType;
            if (typeof(BaseEntity).IsAssignableFrom(alvo)) continue; // filho BaseEntity replica sozinho (uniao)
            if (!ColecoesPocoSubstituicaoAceitas.Contains(alvo))
                problemas.Add($"{et.ClrType.Name}.{nav.Name}: colecao de filho POCO ({alvo.Name}) de agregado " +
                    "GLOBAL nao classificada — PROMOVA a BaseEntity (uniao) OU declare em " +
                    "SyncRegistry.ColecoesPocoSubstituicaoAceitas (substituicao aceita). Sob concorrencia, " +
                    "o delete-missing apagaria o filho do perdedor EM SILENCIO.");
            if (visitados.Add(alvo))
                ColetarPocoNaoClassificado(nav.TargetEntityType, problemas, visitados); // netos POCO
        }
    }
}
