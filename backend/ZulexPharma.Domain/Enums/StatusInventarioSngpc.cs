namespace ZulexPharma.Domain.Enums;

public enum StatusInventarioSngpc
{
    /// <summary>Inventário sendo montado — itens podem ser adicionados/editados, saldos não aplicados.</summary>
    Rascunho = 1,

    /// <summary>Inventário finalizado — ProdutoLote e MovimentoLote foram criados, não pode mais editar.</summary>
    Finalizado = 2
}
