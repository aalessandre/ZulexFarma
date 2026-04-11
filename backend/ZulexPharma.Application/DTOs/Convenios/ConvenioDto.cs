using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Convenios;

public class ConvenioListDto
{
    public long Id { get; set; }
    public long PessoaId { get; set; }
    public string PessoaNome { get; set; } = string.Empty;
    public string? PessoaCpfCnpj { get; set; }
    public string? PessoaTipo { get; set; }
    public string? Aviso { get; set; }
    public ModoFechamento ModoFechamento { get; set; }
    public string ModoFechamentoDescricao { get; set; } = string.Empty;
    public decimal LimiteCredito { get; set; }
    public bool Bloqueado { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class ConvenioDetalheDto
{
    public long Id { get; set; }
    public long PessoaId { get; set; }
    public string PessoaNome { get; set; } = string.Empty;
    public string? PessoaCpfCnpj { get; set; }
    public string? PessoaTipo { get; set; }
    public string? PessoaRazaoSocial { get; set; }
    public string? PessoaIeRg { get; set; }
    public string? Aviso { get; set; }
    public string? Observacao { get; set; }
    public ModoFechamento ModoFechamento { get; set; }
    public int? DiasCorridos { get; set; }
    public int? DiaFechamento { get; set; }
    public int? DiaVencimento { get; set; }
    public int MesesParaVencimento { get; set; }
    public int QtdeViasCupom { get; set; }
    public bool Bloqueado { get; set; }
    public bool PermiteFidelidade { get; set; }
    public bool BloquearVendaParcelada { get; set; }
    public bool BloquearDescontoParcelada { get; set; }
    public bool BloquearComissao { get; set; }
    public bool VenderSomenteComSenha { get; set; }
    public string? SenhaVenda { get; set; }
    public bool CobrarJurosAtraso { get; set; }
    public int DiasCarenciaBloqueio { get; set; }
    public decimal LimiteCredito { get; set; }
    public decimal DescontoGeral { get; set; }
    public int MaximoParcelas { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<ConvenioDescontoDto> Descontos { get; set; } = new();
    public List<ConvenioBloqueioDto> Bloqueios { get; set; } = new();
}

public class ConvenioFormDto
{
    public long PessoaId { get; set; }
    // Campos de pessoa (para criar automaticamente se PessoaId == 0)
    public string? Tipo { get; set; }
    public string? CpfCnpj { get; set; }
    public string? Nome { get; set; }
    public string? RazaoSocial { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Rg { get; set; }
    public string? Aviso { get; set; }
    public string? Observacao { get; set; }
    public ModoFechamento ModoFechamento { get; set; }
    public int? DiasCorridos { get; set; }
    public int? DiaFechamento { get; set; }
    public int? DiaVencimento { get; set; }
    public int MesesParaVencimento { get; set; } = 1;
    public int QtdeViasCupom { get; set; } = 1;
    public bool Bloqueado { get; set; }
    public bool PermiteFidelidade { get; set; }
    public bool BloquearVendaParcelada { get; set; }
    public bool BloquearDescontoParcelada { get; set; }
    public bool BloquearComissao { get; set; }
    public bool VenderSomenteComSenha { get; set; }
    public string? SenhaVenda { get; set; }
    public bool CobrarJurosAtraso { get; set; } = true;
    public int DiasCarenciaBloqueio { get; set; }
    public decimal LimiteCredito { get; set; }
    public decimal DescontoGeral { get; set; }
    public int MaximoParcelas { get; set; } = 1;
    public bool Ativo { get; set; } = true;
    public List<ConvenioDescontoDto> Descontos { get; set; } = new();
    public List<long> BloqueioTipoPagamentoIds { get; set; } = new();
}

public class ConvenioDescontoDto
{
    public long? Id { get; set; }
    public TipoAgrupador TipoAgrupador { get; set; }
    public long AgrupadorId { get; set; }
    public string AgrupadorNome { get; set; } = string.Empty;
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaxSemSenha { get; set; }
    public decimal DescontoMaxComSenha { get; set; }
}

public class ConvenioBloqueioDto
{
    public long TipoPagamentoId { get; set; }
    public string TipoPagamentoNome { get; set; } = string.Empty;
}
