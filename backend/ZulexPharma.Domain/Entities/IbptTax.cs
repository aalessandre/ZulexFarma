namespace ZulexPharma.Domain.Entities;

/// <summary>Tabela IBPTax — alíquotas aproximadas de tributos por NCM (Lei 12.741/2012).</summary>
public class IbptTax
{
    public long Id { get; set; }

    /// <summary>Código NCM (8 dígitos, sem pontos).</summary>
    public string Ncm { get; set; } = string.Empty;

    /// <summary>Exceção tarifária (ex-tipi). Vazio se não houver.</summary>
    public string Ex { get; set; } = string.Empty;

    /// <summary>0=Nacional, 1=Importado, 2=Serviço.</summary>
    public int Tipo { get; set; }

    public string Descricao { get; set; } = string.Empty;

    /// <summary>% tributos federais (produto nacional).</summary>
    public decimal AliqNacional { get; set; }

    /// <summary>% tributos federais (produto importado).</summary>
    public decimal AliqImportado { get; set; }

    /// <summary>% ICMS estadual aproximado.</summary>
    public decimal AliqEstadual { get; set; }

    /// <summary>% ISS municipal (serviços).</summary>
    public decimal AliqMunicipal { get; set; }

    public DateTime VigenciaInicio { get; set; }
    public DateTime VigenciaFim { get; set; }

    /// <summary>Chave de autenticação da versão IBPT.</summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>Versão da tabela (ex: "24.2.A").</summary>
    public string Versao { get; set; } = string.Empty;

    /// <summary>Fonte (ex: "IBPT").</summary>
    public string Fonte { get; set; } = string.Empty;

    /// <summary>UF da alíquota estadual.</summary>
    public string Uf { get; set; } = string.Empty;
}
