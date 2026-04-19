using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Entrega vinculada a uma Venda (1:1). O endereço de destino é persistido inline
/// como snapshot — preserva integridade histórica mesmo se o endereço original
/// for alterado ou removido depois. O campo <see cref="EnderecoEntregaId"/>
/// é apenas referência opcional (audit trail).
/// </summary>
public class Entrega : BaseEntity
{
    public long VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    public long FilialId { get; set; }
    public Filial? Filial { get; set; }

    public long ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    /// <summary>Referência opcional ao endereço original (audit trail). Pode ficar null se apagado.</summary>
    public long? EnderecoEntregaId { get; set; }
    public PessoaEndereco? EnderecoEntrega { get; set; }

    // ── Endereço snapshot (inline — preserva histórico) ────────────
    public string Cep { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string? CodigoIbgeMunicipio { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    /// <summary>Colaborador responsável pela entrega (atribuído no despacho).</summary>
    public long? EntregadorId { get; set; }
    public Colaborador? Entregador { get; set; }

    public StatusEntrega Status { get; set; } = StatusEntrega.Pendente;

    /// <summary>Valor da entrega calculado pela EntregaFaixa vigente.</summary>
    public decimal ValorEntrega { get; set; }

    /// <summary>Distância em km entre filial e endereço (Haversine).</summary>
    public decimal DistanciaKm { get; set; }

    /// <summary>Faixa usada para calcular o valor (auditoria).</summary>
    public long? EntregaFaixaId { get; set; }
    public EntregaFaixa? EntregaFaixa { get; set; }

    /// <summary>Token público usado no link de rastreio do cliente. Sem autenticação.</summary>
    public Guid TokenRastreamento { get; set; } = Guid.NewGuid();

    /// <summary>Token usado pelo entregador pra atualizar status via link (Fase 2 GPS).</summary>
    public Guid TokenEntregador { get; set; } = Guid.NewGuid();

    public DateTime DataPedido { get; set; } = Helpers.DataHoraHelper.Agora();
    public DateTime? DataPrevista { get; set; }
    public DateTime? DataSaida { get; set; }
    public DateTime? DataEntrega { get; set; }

    public string? Observacao { get; set; }

    public ICollection<EntregaEvento> Eventos { get; set; } = new List<EntregaEvento>();
}
