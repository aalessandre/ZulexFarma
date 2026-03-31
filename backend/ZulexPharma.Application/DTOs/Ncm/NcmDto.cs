namespace ZulexPharma.Application.DTOs.Ncm;

public class NcmListDto
{
    public long Id { get; set; }
    public string CodigoNcm { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string? ExTipi { get; set; }
    public string? UnidadeTributavel { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class NcmDetalheDto
{
    public long Id { get; set; }
    public string CodigoNcm { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string? ExTipi { get; set; }
    public string? UnidadeTributavel { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<NcmFederalDto> Federais { get; set; } = new();
    public List<NcmIcmsUfDto> IcmsUfs { get; set; } = new();
    public List<NcmStUfDto> StUfs { get; set; } = new();
}

public class NcmFormDto
{
    public string CodigoNcm { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string? ExTipi { get; set; }
    public string? UnidadeTributavel { get; set; }
    public bool Ativo { get; set; } = true;
    public List<NcmFederalDto> Federais { get; set; } = new();
    public List<NcmIcmsUfDto> IcmsUfs { get; set; } = new();
    public List<NcmStUfDto> StUfs { get; set; } = new();
}

public class NcmFederalDto
{
    public long? Id { get; set; }
    public decimal AliquotaIi { get; set; }
    public decimal AliquotaIpi { get; set; }
    public string? CstIpi { get; set; }
    public decimal AliquotaPis { get; set; }
    public string? CstPis { get; set; }
    public decimal AliquotaCofins { get; set; }
    public string? CstCofins { get; set; }
    public DateTime? VigenciaInicio { get; set; }
    public DateTime? VigenciaFim { get; set; }
}

public class NcmIcmsUfDto
{
    public long? Id { get; set; }
    public string Uf { get; set; } = "";
    public string? CstIcms { get; set; }
    public string? Csosn { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal ReducaoBaseCalculo { get; set; }
    public decimal AliquotaFcp { get; set; }
    public string? Cbenef { get; set; }
    public DateTime? VigenciaInicio { get; set; }
    public DateTime? VigenciaFim { get; set; }
}

public class NcmStUfDto
{
    public long? Id { get; set; }
    public string UfOrigem { get; set; } = "";
    public string UfDestino { get; set; } = "";
    public decimal Mva { get; set; }
    public decimal MvaAjustado { get; set; }
    public decimal AliquotaIcmsSt { get; set; }
    public decimal ReducaoBaseCalculoSt { get; set; }
    public string? Cest { get; set; }
    public DateTime? VigenciaInicio { get; set; }
    public DateTime? VigenciaFim { get; set; }
}
