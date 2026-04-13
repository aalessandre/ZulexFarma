using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Clientes;

public class ClienteListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string Tipo { get; set; } = "F";
    public string CpfCnpj { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public bool Bloqueado { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class ClienteDetalheDto
{
    public long Id { get; set; }
    public long PessoaId { get; set; }
    // Pessoa
    public string Tipo { get; set; } = "F";
    public string Nome { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string CpfCnpj { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? Rg { get; set; }
    public string? DataNascimento { get; set; }
    // Geral
    public decimal LimiteCredito { get; set; }
    public decimal DescontoGeral { get; set; }
    public bool PermiteFidelidade { get; set; }
    public ModoFechamento PrazoPagamento { get; set; }
    public int? QtdeDias { get; set; }
    public int? DiaFechamento { get; set; }
    public int? DiaVencimento { get; set; }
    public int? QtdeMeses { get; set; }
    public bool PermiteVendaParcelada { get; set; }
    public int QtdeMaxParcelas { get; set; }
    public bool PermiteVendaPrazo { get; set; }
    public bool PermiteVendaVista { get; set; }
    public bool Bloqueado { get; set; }
    public bool BloquearDescontoParcelada { get; set; }
    public bool VenderSomenteComSenha { get; set; }
    public bool CobrarJurosAtraso { get; set; }
    public bool BloquearComissao { get; set; }
    public int DiasCarenciaBloqueio { get; set; }
    public bool CalcularJuros { get; set; }
    public bool PedirSenhaVendaPrazo { get; set; }
    public string? SenhaVendaPrazo { get; set; }
    public string? Aviso { get; set; }
    public string? Observacao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    // Sub-tabelas
    public List<EnderecoDto> Enderecos { get; set; } = new();
    public List<ContatoDto> Contatos { get; set; } = new();
    public List<ClienteConvenioDto> Convenios { get; set; } = new();
    public List<ClienteAutorizacaoDto> Autorizacoes { get; set; } = new();
    public List<ClienteDescontoDto> Descontos { get; set; } = new();
    public List<ClienteUsoContinuoDto> UsosContinuos { get; set; } = new();
    public List<ClienteBloqueioDto> Bloqueios { get; set; } = new();
}

public class ClienteFormDto
{
    // Pessoa
    public string Tipo { get; set; } = "F";
    public string Nome { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string CpfCnpj { get; set; } = string.Empty;
    public string? InscricaoEstadual { get; set; }
    public string? Rg { get; set; }
    public string? DataNascimento { get; set; }
    // Geral
    public decimal LimiteCredito { get; set; }
    public decimal DescontoGeral { get; set; }
    public bool PermiteFidelidade { get; set; }
    public ModoFechamento PrazoPagamento { get; set; }
    public int? QtdeDias { get; set; }
    public int? DiaFechamento { get; set; }
    public int? DiaVencimento { get; set; }
    public int? QtdeMeses { get; set; }
    public bool PermiteVendaParcelada { get; set; }
    public int QtdeMaxParcelas { get; set; } = 1;
    public bool PermiteVendaPrazo { get; set; }
    public bool PermiteVendaVista { get; set; } = true;
    public bool Bloqueado { get; set; }
    public bool BloquearDescontoParcelada { get; set; }
    public bool VenderSomenteComSenha { get; set; }
    public bool CobrarJurosAtraso { get; set; } = true;
    public bool BloquearComissao { get; set; }
    public int DiasCarenciaBloqueio { get; set; }
    public bool CalcularJuros { get; set; } = true;
    public bool PedirSenhaVendaPrazo { get; set; }
    public string? SenhaVendaPrazo { get; set; }
    public string? Aviso { get; set; }
    public string? Observacao { get; set; }
    public bool Ativo { get; set; } = true;
    // Sub-tabelas
    public List<EnderecoDto> Enderecos { get; set; } = new();
    public List<ContatoDto> Contatos { get; set; } = new();
    public List<ClienteConvenioDto> Convenios { get; set; } = new();
    public List<ClienteAutorizacaoDto> Autorizacoes { get; set; } = new();
    public List<ClienteDescontoDto> Descontos { get; set; } = new();
    public List<ClienteUsoContinuoDto> UsosContinuos { get; set; } = new();
    public List<long> BloqueioTipoPagamentoIds { get; set; } = new();
}

public class EnderecoDto
{
    public long? Id { get; set; }
    public string Tipo { get; set; } = "CASA";
    public string Cep { get; set; } = string.Empty;
    public string Rua { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public bool Principal { get; set; }
}

public class ContatoDto
{
    public long? Id { get; set; }
    public string Tipo { get; set; } = "TELEFONE";
    public string Valor { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Principal { get; set; }
}

public class ClienteConvenioDto
{
    public long? Id { get; set; }
    public long ConvenioId { get; set; }
    public string? ConvenioNome { get; set; }
    public string? Matricula { get; set; }
    public string? Cartao { get; set; }
}

public class ClienteAutorizacaoDto
{
    public long? Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class ClienteDescontoDto
{
    public long? Id { get; set; }
    public long? ProdutoId { get; set; }
    public TipoAgrupador? TipoAgrupador { get; set; }
    public long? AgrupadorId { get; set; }
    public string AgrupadorOuProdutoNome { get; set; } = string.Empty;
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaxSemSenha { get; set; }
    public decimal DescontoMaxComSenha { get; set; }
}

public class ClienteUsoContinuoDto
{
    public long? Id { get; set; }
    public long ProdutoId { get; set; }
    public string? ProdutoCodigo { get; set; }
    public string? ProdutoNome { get; set; }
    public string? Fabricante { get; set; }
    public int Apresentacao { get; set; }
    public int QtdeAoDia { get; set; }
    public string? UltimaCompra { get; set; }
    public string? ProximaCompra { get; set; }
    public string? ColaboradorNome { get; set; }
}

public class ClienteBloqueioDto
{
    public long TipoPagamentoId { get; set; }
    public string TipoPagamentoNome { get; set; } = string.Empty;
}
