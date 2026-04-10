namespace ZulexPharma.Domain.Entities;

/// <summary>NFC-e emitida.</summary>
public class Nfce : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }
    public long VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    public int Numero { get; set; }
    public int Serie { get; set; }
    public string ChaveAcesso { get; set; } = string.Empty;
    public string? Protocolo { get; set; }
    public DateTime? DataAutorizacao { get; set; }

    /// <summary>1=Produção, 2=Homologação</summary>
    public int Ambiente { get; set; } = 2;

    /// <summary>100=Autorizada, 101=Cancelada, outros=Rejeitada</summary>
    public int CodigoStatus { get; set; }
    public string? MotivoStatus { get; set; }

    /// <summary>XML assinado completo.</summary>
    public string? XmlEnvio { get; set; }
    /// <summary>XML de retorno da SEFAZ.</summary>
    public string? XmlRetorno { get; set; }

    public decimal ValorTotal { get; set; }
}
