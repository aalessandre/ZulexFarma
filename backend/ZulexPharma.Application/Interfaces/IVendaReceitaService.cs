using ZulexPharma.Application.DTOs.Sngpc;

namespace ZulexPharma.Application.Interfaces;

public interface IVendaReceitaService
{
    /// <summary>
    /// Retorna os itens controlados de uma venda e, para cada um, a lista de lotes
    /// disponíveis em estoque na filial — usado para popular a modal SNGPC.
    /// </summary>
    Task<List<ItemControladoDto>> ListarItensControladosAsync(long vendaId);

    /// <summary>
    /// Preview dos itens controlados a partir da lista do carrinho — usado ANTES da venda
    /// ser criada no banco. Permite abrir a tela de receitas sem precisar persistir a venda.
    /// <c>VendaItemId</c> vem zerado no retorno; o frontend usa <c>ProdutoId</c> como chave
    /// temporária até o POST /vendas retornar os ids reais.
    /// </summary>
    Task<List<ItemControladoDto>> ListarItensControladosPreviewAsync(ItensControladosPreviewRequest request);

    /// <summary>
    /// Registra uma ou mais receitas para uma venda (na finalização ou retroativamente).
    /// Para cada receita, baixa o lote informado e grava o vínculo.
    /// </summary>
    Task RegistrarReceitasAsync(long vendaId, List<VendaReceitaFormDto> receitas, long? usuarioId = null);

    /// <summary>
    /// Lista unificada de vendas SNGPC:
    /// <c>pendentes</c> = vendas finalizadas com <c>SngpcPendente=true</c> (sem receitas lançadas);
    /// <c>lancadas</c>  = vendas com ao menos uma VendaReceita gravada;
    /// <c>todas</c>     = união das duas (default).
    /// </summary>
    Task<List<VendaSngpcPendenteDto>> ListarVendasSngpcAsync(string? filtro = null, long? filialId = null, DateTime? dataInicio = null, DateTime? dataFim = null);

    /// <summary>Retorna as receitas já gravadas para uma venda.</summary>
    Task<List<VendaReceitaListDto>> ListarReceitasAsync(long vendaId);

    /// <summary>
    /// Cria uma receita manual (sem venda). Usada para regularização histórica
    /// ou inventário inicial. Aparece em amarelo na lista.
    /// </summary>
    Task<long> RegistrarReceitaManualAsync(VendaReceitaFormDto receita, long filialId, long? usuarioId = null);

    /// <summary>
    /// Busca produtos SNGPC com lotes disponíveis. Usado pelo modal de receita manual.
    /// </summary>
    Task<List<ItemControladoDto>> PesquisarProdutosSngpcAsync(string termo, long filialId);

    /// <summary>
    /// Retorna os itens detalhados (produto + lote + classe) de uma linha da tela Receitas,
    /// seja ela uma venda ou uma receita manual.
    /// </summary>
    Task<List<DetalheReceitaItemDto>> ObterDetalhesAsync(long? vendaId, long? receitaId);
}
