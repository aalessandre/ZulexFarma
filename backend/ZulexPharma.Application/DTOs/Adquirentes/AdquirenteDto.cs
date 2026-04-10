using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Adquirentes;

public class AdquirenteListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int TotalBandeiras { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class AdquirenteDetalheDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<BandeiraDto> Bandeiras { get; set; } = new();
}

public class BandeiraDto
{
    public long Id { get; set; }
    public string Bandeira { get; set; } = string.Empty;
    public List<TarifaDto> Tarifas { get; set; } = new();
}

public class TarifaDto
{
    public long Id { get; set; }
    public ModalidadeCartao Modalidade { get; set; }
    public string ModalidadeDescricao { get; set; } = string.Empty;
    public decimal Tarifa { get; set; }
    public int PrazoRecebimento { get; set; }
    public long? ContaBancariaId { get; set; }
    public string? ContaBancariaNome { get; set; }
}

public class AdquirenteFormDto
{
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public List<BandeiraFormDto> Bandeiras { get; set; } = new();
}

public class BandeiraFormDto
{
    public long? Id { get; set; }
    public string Bandeira { get; set; } = string.Empty;
    public List<TarifaFormDto> Tarifas { get; set; } = new();
}

public class TarifaFormDto
{
    public long? Id { get; set; }
    public ModalidadeCartao Modalidade { get; set; }
    public decimal Tarifa { get; set; }
    public int PrazoRecebimento { get; set; }
    public long? ContaBancariaId { get; set; }
}
