using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Entregas;

public class EntregaListDto
{
    public long Id { get; set; }
    public long VendaId { get; set; }
    public long ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteTelefone { get; set; } = string.Empty;
    public long? EntregadorId { get; set; }
    public string? EntregadorNome { get; set; }
    public StatusEntrega Status { get; set; }
    public string StatusNome { get; set; } = string.Empty;
    public decimal ValorEntrega { get; set; }
    public decimal DistanciaKm { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public DateTime DataPedido { get; set; }
    public DateTime? DataSaida { get; set; }
    public DateTime? DataEntrega { get; set; }
    public Guid TokenRastreamento { get; set; }
}

public class EntregaDetalheDto : EntregaListDto
{
    public long FilialId { get; set; }
    public long? EnderecoEntregaId { get; set; }
    public string Cep { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public long? EntregaFaixaId { get; set; }
    public string? Observacao { get; set; }
    public DateTime? DataPrevista { get; set; }
    public List<EntregaEventoDto> Eventos { get; set; } = new();
}

public class EntregaEventoDto
{
    public long Id { get; set; }
    public TipoEntregaEvento Tipo { get; set; }
    public StatusEntrega? Status { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Texto { get; set; }
    public string? UsuarioLogin { get; set; }
    public DateTime CriadoEm { get; set; }
}

/// <summary>DTO pra iniciar/criar uma entrega vinculada a uma venda.</summary>
public class EntregaFormDto
{
    public long VendaId { get; set; }
    /// <summary>ID do endereço do cliente (busca lat/lng). Se null, backend usa Principal.</summary>
    public long? EnderecoEntregaId { get; set; }
    public string? Observacao { get; set; }
    public DateTime? DataPrevista { get; set; }
}

public class EntregaMudarStatusDto
{
    public StatusEntrega NovoStatus { get; set; }
    public string? Observacao { get; set; }
}

public class EntregaAtribuirEntregadorDto
{
    public long EntregadorId { get; set; }
}

/// <summary>DTO público usado no link de rastreio do cliente — sem dados sensíveis.</summary>
public class EntregaRastreioPublicoDto
{
    public StatusEntrega Status { get; set; }
    public string StatusNome { get; set; } = string.Empty;
    public string FilialNome { get; set; } = string.Empty;
    public string? EntregadorNome { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public decimal DistanciaKm { get; set; }
    public DateTime DataPedido { get; set; }
    public DateTime? DataSaida { get; set; }
    public DateTime? DataEntrega { get; set; }
    public DateTime? DataPrevista { get; set; }
    public List<EntregaEventoPublicoDto> Eventos { get; set; } = new();
}

public class EntregaEventoPublicoDto
{
    public StatusEntrega? Status { get; set; }
    public string? StatusNome { get; set; }
    public DateTime CriadoEm { get; set; }
}

/// <summary>Preview de cálculo antes de efetivar a entrega (modal de confirmação no caixa).</summary>
public class EntregaPreviewDto
{
    public decimal DistanciaKm { get; set; }
    public decimal ValorEntrega { get; set; }
    public long EntregaFaixaId { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}
