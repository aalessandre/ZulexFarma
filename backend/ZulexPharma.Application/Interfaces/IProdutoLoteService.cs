using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Serviço de rastreabilidade por lote.
/// Responsável por criar/atualizar lotes e registrar seus movimentos.
/// </summary>
public interface IProdutoLoteService
{
    /// <summary>
    /// Registra uma entrada de lote na filial (compra, transferência ou ajuste).
    /// Se já existe um lote com mesmo (filial, produto, número, validade), incrementa o saldo.
    /// Senão cria um novo ProdutoLote.
    /// Gera um MovimentoLote do tipo informado.
    /// </summary>
    Task<ProdutoLote> RegistrarEntradaAsync(
        long produtoId,
        long filialId,
        string numeroLote,
        DateTime? dataFabricacao,
        DateTime? dataValidade,
        decimal quantidade,
        TipoMovimentoLote tipo,
        string? registroMs = null,
        long? fornecedorId = null,
        long? compraId = null,
        long? compraProdutoLoteId = null,
        long? usuarioId = null,
        string? observacao = null,
        bool ehLoteFicticio = false);

    /// <summary>
    /// Registra uma saída de lote (venda, perda, transferência).
    /// A escolha do lote (quando múltiplos existem) é responsabilidade do chamador.
    /// </summary>
    Task<MovimentoLote> RegistrarSaidaAsync(
        long produtoLoteId,
        decimal quantidade,
        TipoMovimentoLote tipo,
        long? vendaId = null,
        long? usuarioId = null,
        string? observacao = null);

    /// <summary>
    /// Retorna os lotes de um produto em uma filial com saldo positivo, ordenados por FEFO
    /// (menor data de validade primeiro). Lotes sem validade vão pro final.
    /// </summary>
    Task<List<ProdutoLote>> ListarLotesAtivosAsync(long produtoId, long filialId);

    /// <summary>
    /// Cria lotes fictícios para todos os produtos de um grupo que ainda não têm lote rastreado.
    /// Usado ao ativar <c>ControlarLotesVencimento</c> em um grupo que já tem estoque.
    /// </summary>
    Task<int> GerarLotesFicticiosDoGrupoAsync(long grupoProdutoId, long? usuarioId = null);

    /// <summary>
    /// Cria lotes fictícios para todos os produtos com <c>ClasseTerapeutica</c> psicotrópico/antimicro
    /// que ainda não têm lote rastreado. Usado ao ativar o SNGPC pela primeira vez.
    /// </summary>
    Task<int> GerarLotesFicticiosSngpcAsync(long? usuarioId = null);
}
