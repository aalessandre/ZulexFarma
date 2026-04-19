namespace ZulexPharma.Domain.Enums;

public enum StatusEntrega
{
    /// <summary>Entrega criada, aguardando preparo/despacho.</summary>
    Pendente = 1,
    /// <summary>Em preparação (separando mercadoria).</summary>
    EmPreparacao = 2,
    /// <summary>Entregador saiu pra entrega.</summary>
    SaiuParaEntrega = 3,
    /// <summary>Entregue ao cliente.</summary>
    Entregue = 4,
    /// <summary>Entrega cancelada (não chegou a sair).</summary>
    Cancelada = 5,
    /// <summary>Entregue mas devolvida (cliente recusou / ausente).</summary>
    Devolvida = 6
}
