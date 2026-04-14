namespace ZulexPharma.Domain.Enums;

public enum TipoMovimentoLote
{
    /// <summary>Entrada por compra ou transferência recebida.</summary>
    Entrada = 1,

    /// <summary>Saída por venda normal.</summary>
    Saida = 2,

    /// <summary>Perda (furto, avaria, vencimento, outro).</summary>
    Perda = 3,

    /// <summary>Transferência para outra filial (saída).</summary>
    TransferenciaSaida = 4,

    /// <summary>
    /// Ajuste inicial — lote fictício criado automaticamente ao ativar controle retroativo
    /// (ex: grupo de fraldas passa a ter controle de lotes e o sistema faz snapshot do estoque atual).
    /// </summary>
    AjusteInicial = 5,

    /// <summary>Ajuste manual decorrente de balanço físico.</summary>
    Balanco = 6,

    /// <summary>Estorno de venda (devolução).</summary>
    Estorno = 7
}
