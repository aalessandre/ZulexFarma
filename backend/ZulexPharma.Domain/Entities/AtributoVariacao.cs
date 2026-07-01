namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Eixo de variação global e reusável (ex.: Tamanho, Cor, Voltagem). Cadastrado
/// uma vez e usado por vários produtos. Ver docs/specs/multiramo-grade.md (Passo 2).
/// </summary>
public class AtributoVariacao : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }

    public ICollection<ValorAtributo> Valores { get; set; } = new List<ValorAtributo>();
}
