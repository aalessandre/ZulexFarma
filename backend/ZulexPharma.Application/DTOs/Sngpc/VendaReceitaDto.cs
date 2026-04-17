using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Sngpc;

/// <summary>
/// Dados de uma receita SNGPC a ser registrada no fechamento da venda (ou retroativamente).
/// </summary>
public class VendaReceitaFormDto
{
    public TipoReceitaSngpc Tipo { get; set; }
    public string? NumeroNotificacao { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime DataValidade { get; set; }
    public string? Cid { get; set; }

    /// <summary>
    /// Id do prescritor já cadastrado. Se null, usa os campos de PrescritorNovo e cria cadastro.
    /// </summary>
    public long? PrescritorId { get; set; }
    public PrescritorInlineDto? PrescritorNovo { get; set; }

    public string PacienteNome { get; set; } = string.Empty;
    public string? PacienteCpf { get; set; }
    public string? PacienteRg { get; set; }
    public DateTime? PacienteNascimento { get; set; }
    public string? PacienteSexo { get; set; }
    public string? PacienteEndereco { get; set; }
    public string? PacienteNumero { get; set; }
    public string? PacienteBairro { get; set; }
    public string? PacienteCidade { get; set; }
    public string? PacienteUf { get; set; }
    public string? PacienteCep { get; set; }
    public string? PacienteTelefone { get; set; }

    public bool CompradorMesmoPaciente { get; set; } = true;
    public string? CompradorNome { get; set; }
    public string? CompradorCpf { get; set; }
    public string? CompradorRg { get; set; }
    public string? CompradorEndereco { get; set; }
    public string? CompradorNumero { get; set; }
    public string? CompradorBairro { get; set; }
    public string? CompradorCidade { get; set; }
    public string? CompradorUf { get; set; }
    public string? CompradorCep { get; set; }
    public string? CompradorTelefone { get; set; }

    public List<VendaReceitaItemFormDto> Itens { get; set; } = new();
}

public class PrescritorInlineDto
{
    public string Nome { get; set; } = string.Empty;
    public string TipoConselho { get; set; } = "CRM";
    public string NumeroConselho { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string? Especialidade { get; set; }
}

public class VendaReceitaItemFormDto
{
    /// <summary>Id do VendaItem. Pode ser 0 em receitas manuais (sem venda).</summary>
    public long VendaItemId { get; set; }
    /// <summary>Id do produto — obrigatório em receitas manuais; ignorado se VendaItemId > 0.</summary>
    public long? ProdutoId { get; set; }
    public long ProdutoLoteId { get; set; }
    public decimal Quantidade { get; set; }
}

/// <summary>
/// Payload enviado na finalização da venda (junto com o body tradicional) para já gravar as receitas.
/// </summary>
public class FinalizarVendaSngpcDto
{
    public List<VendaReceitaFormDto> Receitas { get; set; } = new();

    /// <summary>
    /// Se true, marca a venda com SngpcPendente=true. Só permitido quando
    /// <c>sngpc.modo</c> = NaoLancar ou Misto (este último exige token de liberação).
    /// </summary>
    public bool LancarDepois { get; set; }
}

public class VendaReceitaListDto
{
    public long Id { get; set; }
    public long? VendaId { get; set; }
    public TipoReceitaSngpc Tipo { get; set; }
    public string? NumeroNotificacao { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime DataValidade { get; set; }
    public string PrescritorNome { get; set; } = string.Empty;
    public string PacienteNome { get; set; } = string.Empty;
    public int QtdeItens { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class VendaSngpcPendenteDto
{
    /// <summary>Id da venda. Null quando origem = "Manual" (receita avulsa).</summary>
    public long? VendaId { get; set; }
    /// <summary>Id da receita manual (preenchido apenas quando Origem = "Manual").</summary>
    public long? ReceitaId { get; set; }
    public string? Codigo { get; set; }
    public DateTime? DataFinalizacao { get; set; }
    public string? ClienteNome { get; set; }
    public int QtdeItensControlados { get; set; }
    public decimal QtdeTotal { get; set; }
    /// <summary>"Pendente" (venda sem receita) | "Lançada" (venda c/ receita) | "Manual" (receita solta).</summary>
    public string Status { get; set; } = "Pendente";
    /// <summary>Quantidade de receitas já gravadas.</summary>
    public int QtdeReceitas { get; set; }
}

public class ItemControladoDto
{
    public long VendaItemId { get; set; }
    public long ProdutoId { get; set; }
    public string ProdutoNome { get; set; } = string.Empty;
    public string? ClasseTerapeutica { get; set; }
    public int Quantidade { get; set; }
    public List<LoteDisponivelDto> LotesDisponiveis { get; set; } = new();
}

/// <summary>Item detalhado exibido ao expandir uma linha na tela de Receitas.</summary>
public class DetalheReceitaItemDto
{
    public string ProdutoNome { get; set; } = string.Empty;
    public string? ClasseTerapeutica { get; set; }
    public decimal Quantidade { get; set; }
    public string? NumeroLote { get; set; }
    public DateTime? DataValidade { get; set; }
    /// <summary>"Vendido" (saiu pelo cart), "Na receita" (cobre a venda), "Manual" (receita avulsa).</summary>
    public string Origem { get; set; } = "Vendido";
}

/// <summary>Request de preview: recebe itens do cart antes da venda existir no banco.</summary>
public class ItensControladosPreviewRequest
{
    public long FilialId { get; set; }
    public List<ItemPreviewRequest> Itens { get; set; } = new();
}

public class ItemPreviewRequest
{
    public long ProdutoId { get; set; }
    public int Quantidade { get; set; }
}

public class LoteDisponivelDto
{
    public long ProdutoLoteId { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime? DataValidade { get; set; }
    public decimal SaldoAtual { get; set; }
}
