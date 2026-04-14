using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Caixa;

public class CaixaMovimentoListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public long CaixaId { get; set; }
    public long? VendaId { get; set; }
    public TipoMovimentoCaixa Tipo { get; set; }
    public string TipoDescricao { get; set; } = string.Empty;
    public DateTime DataMovimento { get; set; }
    public decimal Valor { get; set; }
    public long? TipoPagamentoId { get; set; }
    public string? TipoPagamentoNome { get; set; }
    public int? ModalidadePagamento { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Observacao { get; set; }
    public StatusConferenciaMovimento StatusConferencia { get; set; }
    public string StatusConferenciaDescricao { get; set; } = string.Empty;
    public DateTime? DataConferencia { get; set; }
    public long? ConferidoPorUsuarioId { get; set; }
    public string? ConferidoPorUsuarioNome { get; set; }
    public long? ConferenteUsuarioId { get; set; }
    public string? ConferenteUsuarioNome { get; set; }
    public DateTime? DataConferenteSangria { get; set; }
    public string? UsuarioNome { get; set; }
}

public class SangriaFormDto
{
    public long CaixaId { get; set; }
    public decimal Valor { get; set; }
    public string? Observacao { get; set; }
}

public class SuprimentoFormDto
{
    public long CaixaId { get; set; }
    public decimal Valor { get; set; }
    public string? Observacao { get; set; }
}

public class RecebimentoFormDto
{
    public long CaixaId { get; set; }
    public long ContaReceberId { get; set; }
    public decimal Valor { get; set; }
    public long TipoPagamentoId { get; set; }
    public string? Observacao { get; set; }
}

public class PagamentoFormDto
{
    public long CaixaId { get; set; }
    public long PessoaId { get; set; }
    public long PlanoContaId { get; set; }
    public decimal Valor { get; set; }
    public long TipoPagamentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Observacao { get; set; }
}

public class BiparCanhotoFormDto
{
    public string Codigo { get; set; } = string.Empty;
}

public class FechamentoDeclaradoDto
{
    public long TipoPagamentoId { get; set; }
    public decimal ValorDeclarado { get; set; }
}

public class FechamentoFormDto
{
    public long CaixaId { get; set; }
    public List<FechamentoDeclaradoDto> Declarados { get; set; } = new();
    public string? Observacao { get; set; }
}
