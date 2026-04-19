using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Feriado com âmbito (Nacional/Estadual/Municipal).
/// Usado pelo EntregaService pra aplicar perfil de "Feriado" na agenda (RN-07).
///
/// Unicidade: (Data, Ambito, Uf, FilialId) — não duplicar o mesmo feriado.
/// </summary>
public class Feriado : BaseEntity
{
    /// <summary>Data do feriado (apenas data, sem hora). Armazenado como 'date'.</summary>
    public DateOnly Data { get; set; }

    /// <summary>Nome do feriado (ex: "NATAL", "PROCLAMAÇÃO DA REPÚBLICA").</summary>
    public string Nome { get; set; } = string.Empty;

    public AmbitoFeriado Ambito { get; set; }

    /// <summary>Obrigatório quando Ambito = Estadual. 2 chars, UPPERCASE.</summary>
    public string? Uf { get; set; }

    /// <summary>Obrigatório quando Ambito = Municipal. FK pra Filial (município herdado dela).</summary>
    public long? FilialId { get; set; }
    public Filial? Filial { get; set; }

    public OrigemFeriado Origem { get; set; } = OrigemFeriado.Manual;
}
