using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Ledger universal de estoque. <see cref="Registrar"/> enfileira um movimento no MESMO
/// DbContext do chamador (NAO chama SaveChanges — quem move o estoque persiste tudo junto,
/// atomico). <see cref="ListarPorProdutoAsync"/> devolve o extrato pra tela de Movimentacao.
/// </summary>
public interface IMovimentoEstoqueService
{
    void Registrar(long produtoId, long filialId, long? variacaoId, decimal delta, decimal saldoApos,
        TipoMovimentoEstoque tipo, string? documento = null, long? pessoaId = null, string? pessoaNome = null,
        long? usuarioId = null, long? compraId = null, long? vendaId = null, string? observacao = null);

    Task<List<MovimentoEstoqueDto>> ListarPorProdutoAsync(long produtoId, long? filialId,
        DateTime? dataInicio, DateTime? dataFim);
}
