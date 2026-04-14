namespace ZulexPharma.Application.DTOs.Sngpc;

// ══ Inventário SNGPC ═══════════════════════════════════════════════
public class InventarioSngpcListDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public string? FilialNome { get; set; }
    public DateTime DataInventario { get; set; }
    public string? Descricao { get; set; }
    public int Status { get; set; }
    public string StatusNome { get; set; } = "";
    public DateTime? DataFinalizacao { get; set; }
    public int TotalItens { get; set; }
    public decimal QuantidadeTotal { get; set; }
}

public class InventarioSngpcDetalheDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public DateTime DataInventario { get; set; }
    public string? Descricao { get; set; }
    public int Status { get; set; }
    public DateTime? DataFinalizacao { get; set; }
    public string? Observacao { get; set; }
    public List<InventarioSngpcItemDto> Itens { get; set; } = new();
}

public class InventarioSngpcItemDto
{
    public long? Id { get; set; }
    public long ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public string? ProdutoCodigoBarras { get; set; }
    public string? ClasseTerapeutica { get; set; }
    public string NumeroLote { get; set; } = "";
    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }
    public decimal Quantidade { get; set; }
    public string? RegistroMs { get; set; }
    public string? Observacao { get; set; }
}

public class InventarioSngpcFormDto
{
    public long FilialId { get; set; }
    public DateTime DataInventario { get; set; }
    public string? Descricao { get; set; }
    public string? Observacao { get; set; }
    public List<InventarioSngpcItemDto> Itens { get; set; } = new();
}

// ══ Receita ════════════════════════════════════════════════════════
public class ReceitaListDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public long? VendaId { get; set; }
    public string MedicoNome { get; set; } = "";
    public string? MedicoCrm { get; set; }
    public string PacienteNome { get; set; } = "";
    public string? NumeroReceita { get; set; }
    public DateTime DataEmissao { get; set; }
    public string? TipoReceita { get; set; }
    public int TotalItens { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class ReceitaDetalheDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public long? VendaId { get; set; }
    public string MedicoNome { get; set; } = "";
    public string? MedicoCrm { get; set; }
    public string? MedicoUf { get; set; }
    public string? MedicoCpf { get; set; }
    public string PacienteNome { get; set; } = "";
    public string? PacienteCpf { get; set; }
    public string? PacienteEndereco { get; set; }
    public string? PacienteCep { get; set; }
    public string? PacienteCidade { get; set; }
    public string? PacienteUf { get; set; }
    public string? NumeroReceita { get; set; }
    public DateTime DataEmissao { get; set; }
    public string? TipoReceita { get; set; }
    public string? Observacao { get; set; }
    public List<ReceitaItemDto> Itens { get; set; } = new();
}

public class ReceitaItemDto
{
    public long? Id { get; set; }
    public long ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public long? ProdutoLoteId { get; set; }
    public string? NumeroLote { get; set; }
    public decimal Quantidade { get; set; }
    public string? Posologia { get; set; }
}

public class ReceitaFormDto
{
    public long FilialId { get; set; }
    public long? VendaId { get; set; }
    public string MedicoNome { get; set; } = "";
    public string? MedicoCrm { get; set; }
    public string? MedicoUf { get; set; }
    public string? MedicoCpf { get; set; }
    public string PacienteNome { get; set; } = "";
    public string? PacienteCpf { get; set; }
    public string? PacienteEndereco { get; set; }
    public string? PacienteCep { get; set; }
    public string? PacienteCidade { get; set; }
    public string? PacienteUf { get; set; }
    public string? NumeroReceita { get; set; }
    public DateTime DataEmissao { get; set; }
    public string? TipoReceita { get; set; }
    public string? Observacao { get; set; }
    public List<ReceitaItemDto> Itens { get; set; } = new();
}

// ══ Perda ══════════════════════════════════════════════════════════
public class PerdaListDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public long ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public long ProdutoLoteId { get; set; }
    public string? NumeroLote { get; set; }
    public DateTime? DataValidade { get; set; }
    public decimal Quantidade { get; set; }
    public DateTime DataPerda { get; set; }
    public int Motivo { get; set; }
    public string MotivoNome { get; set; } = "";
    public string? NumeroBoletim { get; set; }
    public string? Observacao { get; set; }
    public string? UsuarioNome { get; set; }
}

public class PerdaFormDto
{
    public long FilialId { get; set; }
    public long ProdutoId { get; set; }
    public long ProdutoLoteId { get; set; }
    public decimal Quantidade { get; set; }
    public DateTime DataPerda { get; set; }
    public int Motivo { get; set; }
    public string? NumeroBoletim { get; set; }
    public string? Observacao { get; set; }
}

// ══ Estoque SNGPC ══════════════════════════════════════════════════
public class EstoqueSngpcLinhaDto
{
    public long ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = "";
    public string? ProdutoCodigoBarras { get; set; }
    public string? ClasseTerapeutica { get; set; }
    public long ProdutoLoteId { get; set; }
    public string NumeroLote { get; set; } = "";
    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }
    public decimal SaldoAtual { get; set; }
    public bool EhLoteFicticio { get; set; }
    public int DiasParaVencer { get; set; }    // negativo = vencido
}

// ══ Compras e Transferências SNGPC ═════════════════════════════════
public class CompraSngpcListDto
{
    public long CompraId { get; set; }
    public string? Codigo { get; set; }
    public string NumeroNf { get; set; } = "";
    public string FornecedorNome { get; set; } = "";
    public DateTime? DataEmissao { get; set; }
    public DateTime? DataFinalizacao { get; set; }
    public int QtdeProdutosSngpc { get; set; }
    public int QtdeLotesCriados { get; set; }
    public decimal QuantidadeTotal { get; set; }
    public bool SngpcOptOut { get; set; }
    public string StatusSngpc { get; set; } = ""; // "Lançada", "Opt-out"
}

public class CompraSngpcDetalheDto
{
    public long CompraId { get; set; }
    public string NumeroNf { get; set; } = "";
    public string FornecedorNome { get; set; } = "";
    public DateTime? DataEmissao { get; set; }
    public DateTime? DataFinalizacao { get; set; }
    public bool SngpcOptOut { get; set; }
    public List<CompraSngpcItemDto> Itens { get; set; } = new();
}

public class CompraSngpcItemDto
{
    public long ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = "";
    public string? ClasseTerapeutica { get; set; }
    public long? ProdutoLoteId { get; set; }
    public string NumeroLote { get; set; } = "";
    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }
    public decimal Quantidade { get; set; }
    public DateTime DataEntrada { get; set; }
}

// ══ Mapa SNGPC ═════════════════════════════════════════════════════
public class SngpcMapaListDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public int CompetenciaMes { get; set; }
    public int CompetenciaAno { get; set; }
    public int Status { get; set; }
    public string StatusNome { get; set; } = "";
    public DateTime? DataGeracao { get; set; }
    public DateTime? DataEnvio { get; set; }
    public string? ProtocoloAnvisa { get; set; }
    public int TotalEntradas { get; set; }
    public int TotalSaidas { get; set; }
    public int TotalReceitas { get; set; }
    public int TotalPerdas { get; set; }
}

public class GerarMapaSngpcRequest
{
    public long FilialId { get; set; }
    public int Mes { get; set; }
    public int Ano { get; set; }
}
