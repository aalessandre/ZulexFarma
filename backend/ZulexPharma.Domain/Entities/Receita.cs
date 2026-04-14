namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Dados da receita médica apresentada ao dispensar medicamentos controlados.
/// Uma receita pode cobrir múltiplos itens de uma mesma venda.
/// Obrigatória para compor o relatório SNGPC (tipo de receita corresponde à lista da Portaria 344).
/// </summary>
public class Receita : BaseEntity
{
    public long FilialId { get; set; }

    /// <summary>Venda atendida por esta receita (opcional — pode ser cadastrada independente).</summary>
    public long? VendaId { get; set; }
    public Venda? Venda { get; set; }

    // ── Prescritor (médico/dentista) ────────────────────────────────
    public string MedicoNome { get; set; } = string.Empty;
    public string? MedicoCrm { get; set; }
    public string? MedicoUf { get; set; }
    public string? MedicoCpf { get; set; }

    // ── Paciente ────────────────────────────────────────────────────
    public string PacienteNome { get; set; } = string.Empty;
    public string? PacienteCpf { get; set; }
    public string? PacienteEndereco { get; set; }
    public string? PacienteCep { get; set; }
    public string? PacienteCidade { get; set; }
    public string? PacienteUf { get; set; }

    // ── Receita ────────────────────────────────────────────────────
    public string? NumeroReceita { get; set; }
    public DateTime DataEmissao { get; set; }
    /// <summary>Tipo conforme Portaria 344: "Amarela", "Azul", "Azul B2", "Branca", "Especial".</summary>
    public string? TipoReceita { get; set; }
    public string? Observacao { get; set; }

    public ICollection<ReceitaItem> Itens { get; set; } = new List<ReceitaItem>();
}

public class ReceitaItem : BaseEntity
{
    public long ReceitaId { get; set; }
    public Receita Receita { get; set; } = null!;

    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public long? ProdutoLoteId { get; set; }
    public ProdutoLote? ProdutoLote { get; set; }

    public decimal Quantidade { get; set; }
    public string? Posologia { get; set; }
}
