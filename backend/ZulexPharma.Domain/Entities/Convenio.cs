using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class Convenio : BaseEntity
{
    public long PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    public string? Aviso { get; set; }
    public string? Observacao { get; set; }

    // ── Fechamento ─────────────────────────────────────────────
    public ModoFechamento ModoFechamento { get; set; } = ModoFechamento.DiasCorridos;
    public int? DiasCorridos { get; set; }
    public int? DiaFechamento { get; set; }
    public int? DiaVencimento { get; set; }
    public int MesesParaVencimento { get; set; } = 1;

    // ── Regras ─────────────────────────────────────────────────
    public int QtdeViasCupom { get; set; } = 1;
    public bool Bloqueado { get; set; }
    public bool PermiteFidelidade { get; set; }
    public bool BloquearVendaParcelada { get; set; }
    public bool BloquearDescontoParcelada { get; set; }
    public bool BloquearComissao { get; set; }
    public bool VenderSomenteComSenha { get; set; }
    public string? SenhaVenda { get; set; }
    public bool CobrarJurosAtraso { get; set; } = true;
    public int DiasCarenciaBloqueio { get; set; }

    // ── Limites ────────────────────────────────────────────────
    public decimal LimiteCredito { get; set; }
    public decimal DescontoGeral { get; set; }
    public int MaximoParcelas { get; set; } = 1;

    // ── Navigation ─────────────────────────────────────────────
    public ICollection<ConvenioDesconto> Descontos { get; set; } = new List<ConvenioDesconto>();
    public ICollection<ConvenioBloqueio> Bloqueios { get; set; } = new List<ConvenioBloqueio>();
}
