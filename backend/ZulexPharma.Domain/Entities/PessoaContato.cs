namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Contatos de uma Pessoa: telefone, celular, e-mail, WhatsApp, etc.
/// Uma pessoa pode ter múltiplos contatos.
/// </summary>
public class PessoaContato : BaseEntity
{
    public long PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;

    /// <summary>TELEFONE | CELULAR | EMAIL | WHATSAPP | OUTRO</summary>
    public string Tipo { get; set; } = string.Empty;

    public string Valor { get; set; } = string.Empty;

    /// <summary>Ex.: "Comercial", "Pessoal", "RH"</summary>
    public string? Descricao { get; set; }

    public bool Principal { get; set; } = false;
}
