using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.PlanosContas;

public class PlanoContaListDto
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public NivelConta Nivel { get; set; }
    public string NivelDescricao { get; set; } = string.Empty;
    public NaturezaConta Natureza { get; set; }
    public string NaturezaDescricao { get; set; } = string.Empty;
    public long? ContaPaiId { get; set; }
    public string? ContaPaiDescricao { get; set; }
    public int Ordem { get; set; }
    public string CodigoHierarquico { get; set; } = string.Empty;
    public bool VisivelRelatorio { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class PlanoContaFormDto
{
    public string Descricao { get; set; } = string.Empty;
    public NivelConta Nivel { get; set; }
    public NaturezaConta Natureza { get; set; }
    public long? ContaPaiId { get; set; }
    public int Ordem { get; set; } = 1;
    public bool VisivelRelatorio { get; set; } = true;
    public bool Ativo { get; set; } = true;
}
