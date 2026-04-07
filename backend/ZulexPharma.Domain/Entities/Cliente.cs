using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class Cliente : BaseEntity
{
    public long PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    // ── Geral ──────────────────────────────────────────────────
    public decimal LimiteCredito { get; set; }
    public decimal DescontoGeral { get; set; }
    public bool PermiteFidelidade { get; set; }
    public ModoFechamento PrazoPagamento { get; set; } = ModoFechamento.DiasCorridos;
    public int? QtdeDias { get; set; }
    public int? DiaFechamento { get; set; }
    public int? DiaVencimento { get; set; }
    public int? QtdeMeses { get; set; }
    public bool PermiteVendaParcelada { get; set; }
    public int QtdeMaxParcelas { get; set; } = 1;
    public bool PermiteVendaPrazo { get; set; }
    public bool PermiteVendaVista { get; set; } = true;
    public bool Bloqueado { get; set; }
    public bool CalcularJuros { get; set; } = true;
    public bool BloquearComissao { get; set; }
    public bool PedirSenhaVendaPrazo { get; set; }
    public string? SenhaVendaPrazo { get; set; }
    public string? Aviso { get; set; }
    public string? Observacao { get; set; }

    // ── Navigation ─────────────────────────────────────────────
    public ICollection<ClienteConvenio> Convenios { get; set; } = new List<ClienteConvenio>();
    public ICollection<ClienteAutorizacao> Autorizacoes { get; set; } = new List<ClienteAutorizacao>();
    public ICollection<ClienteDesconto> Descontos { get; set; } = new List<ClienteDesconto>();
    public ICollection<ClienteUsoContinuo> UsosContinuos { get; set; } = new List<ClienteUsoContinuo>();
}
