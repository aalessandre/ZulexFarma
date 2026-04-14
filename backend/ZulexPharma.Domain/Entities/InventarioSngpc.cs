using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Inventário inicial SNGPC (e posteriores balanços físicos).
/// Usado para estabelecer o saldo inicial dos produtos controlados antes de começar a operar
/// o SNGPC, ou para ajustar divergências após balanço.
///
/// Fluxo:
///  1. Usuário cria o inventário (status Rascunho)
///  2. Adiciona itens (produto + lote + quantidade) um por um
///  3. Finaliza → para cada item, cria/incrementa ProdutoLote e gera MovimentoLote tipo AjusteInicial
/// </summary>
public class InventarioSngpc : BaseEntity
{
    public long FilialId { get; set; }
    public DateTime DataInventario { get; set; }
    public string? Descricao { get; set; }
    public StatusInventarioSngpc Status { get; set; } = StatusInventarioSngpc.Rascunho;
    public DateTime? DataFinalizacao { get; set; }

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string? Observacao { get; set; }

    public ICollection<InventarioSngpcItem> Itens { get; set; } = new List<InventarioSngpcItem>();
}

public class InventarioSngpcItem : BaseEntity
{
    public long InventarioSngpcId { get; set; }
    public InventarioSngpc Inventario { get; set; } = null!;

    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public string NumeroLote { get; set; } = string.Empty;
    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }
    public decimal Quantidade { get; set; }
    public string? RegistroMs { get; set; }
    public string? Observacao { get; set; }
}
