namespace ZulexPharma.Application.DTOs.Vendas;

/// <summary>Resultado da validação de venda a prazo.</summary>
public class VendaPrazoValidacaoDto
{
    public bool ClienteBloqueado { get; set; }
    public bool ConvenioBloqueado { get; set; }
    public string? MensagemBloqueio { get; set; }

    public decimal LimiteCredito { get; set; }
    public decimal SaldoUtilizado { get; set; }
    public decimal SaldoDisponivel { get; set; }
    public bool ExcedeLimite { get; set; }

    public bool PermiteParcelada { get; set; }
    public int MaxParcelas { get; set; }
    public bool BloquearDescontoParcelada { get; set; }

    public bool ExigeSenha { get; set; }

    public bool TipoPagamentoBloqueado { get; set; }
    public string? MensagemTipoBloqueado { get; set; }
}

/// <summary>Body para POST api/vendas/{id}/finalizar.</summary>
public class FinalizarVendaDto
{
    public string? SenhaCliente { get; set; }
    public string? TokenLiberacaoCredito { get; set; }
    public int NumeroParcelas { get; set; } = 1;

    /// <summary>Payload SNGPC quando a venda tem itens controlados (psicotrópicos/antimicrobianos).</summary>
    public ZulexPharma.Application.DTOs.Sngpc.FinalizarVendaSngpcDto? Sngpc { get; set; }

    /// <summary>Dados de entrega. Se preenchido, cria Entrega vinculada à venda.</summary>
    public EntregaFinalizacaoDto? Entrega { get; set; }

    /// <summary>
    /// True = caixa contabiliza agora (cliente pagou); false = contabilização diferida (contabiliza na baixa da entrega).
    /// Default true. Só faz sentido false quando há entrega.
    /// </summary>
    public bool PagamentoRecebido { get; set; } = true;
}

public class EntregaFinalizacaoDto
{
    /// <summary>Endereço escolhido (obrigatório; vem do modal de entrega).</summary>
    public long EnderecoEntregaId { get; set; }
    public string? Observacao { get; set; }
    /// <summary>Se true, já cria entrega com status SaiuParaEntrega + entregador atribuído.</summary>
    public bool DespacharAgora { get; set; }
    /// <summary>Obrigatório quando DespacharAgora=true.</summary>
    public long? EntregadorId { get; set; }
}

/// <summary>Request para POST api/vendas/validar-prazo.</summary>
public class ValidarPrazoRequestDto
{
    public long ClienteId { get; set; }
    public long? ConvenioId { get; set; }
    public long TipoPagamentoId { get; set; }
    public decimal ValorVenda { get; set; }
}
