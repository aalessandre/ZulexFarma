using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class Promocao : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public TipoPromocao Tipo { get; set; } = TipoPromocao.Fixa;
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }

    /// <summary>Bitmask: Dom=1, Seg=2, Ter=4, Qua=8, Qui=16, Sex=32, Sab=64. 127=todos.</summary>
    public int DiaSemana { get; set; } = 127;

    public bool PermitirMudarPreco { get; set; }
    public bool GerarComissao { get; set; }
    public bool ExclusivaConvenio { get; set; }
    public decimal ReducaoVendaPrazo { get; set; }
    public int? QtdeMaxPorVenda { get; set; }

    // ── Lançar por quantidade (fixa) ─────────────────────────────
    public bool LancarPorQuantidade { get; set; }
    public DateTime? DataInicioContagem { get; set; }

    // ── Progressiva ────────────────────────────────────────────
    public bool Intersabores { get; set; }

    // ── Navigation ─────────────────────────────────────────────
    public ICollection<PromocaoFaixa> Faixas { get; set; } = new List<PromocaoFaixa>();
    public ICollection<PromocaoFilial> Filiais { get; set; } = new List<PromocaoFilial>();
    public ICollection<PromocaoPagamento> Pagamentos { get; set; } = new List<PromocaoPagamento>();
    public ICollection<PromocaoConvenio> Convenios { get; set; } = new List<PromocaoConvenio>();
    public ICollection<PromocaoProduto> Produtos { get; set; } = new List<PromocaoProduto>();
}
