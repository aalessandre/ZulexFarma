using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.TiposPagamento;

public class TipoPagamentoListDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public ModalidadePagamento Modalidade { get; set; }
    public string ModalidadeDescricao { get; set; } = string.Empty;
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaxSemSenha { get; set; }
    public decimal DescontoMaxComSenha { get; set; }
    public bool AceitaPromocao { get; set; }
    public int Ordem { get; set; }
    public bool PadraoSistema { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class TipoPagamentoFormDto
{
    public string Nome { get; set; } = string.Empty;
    public ModalidadePagamento Modalidade { get; set; }
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaxSemSenha { get; set; }
    public decimal DescontoMaxComSenha { get; set; }
    public bool AceitaPromocao { get; set; } = true;
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;
}
