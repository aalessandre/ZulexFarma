using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Registra entradas e saídas nas contas bancárias (incluindo a Conta Cofre da filial).
/// </summary>
public class MovimentoContaBancaria : BaseEntity
{
    public long ContaBancariaId { get; set; }
    public ContaBancaria ContaBancaria { get; set; } = null!;

    public DateTime DataMovimento { get; set; }
    public TipoMovimentoBancario Tipo { get; set; }
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Vínculo opcional com o movimento do caixa que originou este lançamento.</summary>
    public long? CaixaMovimentoId { get; set; }
    public CaixaMovimento? CaixaMovimento { get; set; }

    /// <summary>Vínculo opcional com o caixa (para relatórios).</summary>
    public long? CaixaId { get; set; }
    public Caixa? Caixa { get; set; }

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
