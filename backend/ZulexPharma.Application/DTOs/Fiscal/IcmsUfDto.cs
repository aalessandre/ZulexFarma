namespace ZulexPharma.Application.DTOs.Fiscal;

public class IcmsUfListDto
{
    public long Id { get; set; }
    public string Uf { get; set; } = "";
    public string NomeEstado { get; set; } = "";
    public decimal AliquotaInterna { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class IcmsUfFormDto
{
    public string Uf { get; set; } = "";
    public string NomeEstado { get; set; } = "";
    public decimal AliquotaInterna { get; set; }
    public bool Ativo { get; set; } = true;
}
