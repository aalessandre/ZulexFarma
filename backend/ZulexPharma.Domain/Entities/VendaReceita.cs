using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Receita SNGPC vinculada a uma venda. Uma venda pode ter múltiplas receitas,
/// e cada receita pode cobrir múltiplos itens controlados.
/// Paciente e comprador são gravados como texto livre (sem FK) para evitar
/// cadastros aleatórios — o vendedor digita a cada venda.
/// </summary>
public class VendaReceita : BaseEntity
{
    public long VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    public TipoReceitaSngpc Tipo { get; set; }

    /// <summary>Número da notificação (obrigatório p/ tipos A, B1, B2; opcional nos demais).</summary>
    public string? NumeroNotificacao { get; set; }

    public DateTime DataEmissao { get; set; }
    public DateTime DataValidade { get; set; }

    public string? Cid { get; set; }

    public long PrescritorId { get; set; }
    public Prescritor Prescritor { get; set; } = null!;

    // ── Paciente (texto livre, sem cadastro) ──────────────────────
    public string PacienteNome { get; set; } = string.Empty;
    public string? PacienteCpf { get; set; }
    public string? PacienteRg { get; set; }
    public DateTime? PacienteNascimento { get; set; }
    public string? PacienteSexo { get; set; }
    public string? PacienteEndereco { get; set; }
    public string? PacienteNumero { get; set; }
    public string? PacienteBairro { get; set; }
    public string? PacienteCidade { get; set; }
    public string? PacienteUf { get; set; }
    public string? PacienteCep { get; set; }
    public string? PacienteTelefone { get; set; }

    /// <summary>Quando true, o comprador é o próprio paciente (default).</summary>
    public bool CompradorMesmoPaciente { get; set; } = true;

    // ── Comprador (preenche apenas se CompradorMesmoPaciente = false) ──
    public string? CompradorNome { get; set; }
    public string? CompradorCpf { get; set; }
    public string? CompradorRg { get; set; }
    public string? CompradorEndereco { get; set; }
    public string? CompradorNumero { get; set; }
    public string? CompradorBairro { get; set; }
    public string? CompradorCidade { get; set; }
    public string? CompradorUf { get; set; }
    public string? CompradorCep { get; set; }
    public string? CompradorTelefone { get; set; }

    public ICollection<VendaReceitaItem> Itens { get; set; } = new List<VendaReceitaItem>();
}
