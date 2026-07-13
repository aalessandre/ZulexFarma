using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Ledger universal e imutavel de estoque: uma linha por alteracao de
/// <see cref="ProdutoDados.EstoqueAtual"/> (compra, venda, perda, transferencia,
/// ajuste manual, grade). Desacoplado (so' ids puros, SEM navigation/FK) pra que a
/// gravacao nunca quebre o fluxo de estoque e nao bloqueie exclusoes por FK.
/// Nome de pessoa e' snapshot; o saldo pos-movimento e' carimbado na hora.
/// </summary>
public class MovimentoEstoque : BaseEntity
{
    public long ProdutoId { get; set; }
    public long FilialId { get; set; }
    /// <summary>SKU de grade; null = estoque simples (linha-base).</summary>
    public long? ProdutoVariacaoId { get; set; }

    public DateTime Data { get; set; } = Helpers.DataHoraHelper.Agora();
    public TipoMovimentoEstoque Tipo { get; set; }

    /// <summary>Delta COM SINAL: +entrada, -saida.</summary>
    public decimal Quantidade { get; set; }
    /// <summary>EstoqueAtual da linha (produto+filial+variacao) DEPOIS do movimento.</summary>
    public decimal SaldoApos { get; set; }

    /// <summary>Documento de origem (NF, codigo da venda, etc.).</summary>
    public string? Documento { get; set; }
    /// <summary>Fornecedor/cliente/destinatario (id de referencia, sem FK).</summary>
    public long? PessoaId { get; set; }
    /// <summary>Nome da pessoa no momento do movimento (snapshot).</summary>
    public string? PessoaNome { get; set; }
    /// <summary>Usuario que originou o movimento (resolve o nome por join na leitura).</summary>
    public long? UsuarioId { get; set; }

    public long? CompraId { get; set; }
    public long? VendaId { get; set; }
    public string? Observacao { get; set; }
}
