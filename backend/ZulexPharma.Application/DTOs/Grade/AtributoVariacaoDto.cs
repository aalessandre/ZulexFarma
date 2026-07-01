namespace ZulexPharma.Application.DTOs.Grade;

public class ValorAtributoDto
{
    public long Id { get; set; }
    public string Valor { get; set; } = string.Empty;
    public string? Hex { get; set; }
    public int Ordem { get; set; }
}

public class AtributoVariacaoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Ativo { get; set; }
    public List<ValorAtributoDto> Valores { get; set; } = new();
}

public class ValorAtributoFormDto
{
    /// <summary>Null = valor novo; preenchido = valor existente (atualizar).</summary>
    public long? Id { get; set; }
    public string Valor { get; set; } = string.Empty;
    public string? Hex { get; set; }
    public int Ordem { get; set; }
}

public class AtributoVariacaoFormDto
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;
    public List<ValorAtributoFormDto> Valores { get; set; } = new();
}
