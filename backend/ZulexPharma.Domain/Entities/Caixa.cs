using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class Caixa : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }
    public long ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;
    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTime DataAbertura { get; set; }
    public DateTime? DataFechamento { get; set; }
    public DateTime? DataConferencia { get; set; }

    public decimal ValorAbertura { get; set; }
    public CaixaStatus Status { get; set; } = CaixaStatus.Aberto;
    public string? Observacao { get; set; }
}
