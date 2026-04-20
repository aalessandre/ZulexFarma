namespace ZulexPharma.Domain.Enums;

public enum FaseFarmaciaPopular
{
    /// <summary>Fase 1: executarSolicitacao.</summary>
    Solicitacao = 1,
    /// <summary>Fase 2: confirmarAutorizacao.</summary>
    Confirmacao = 2,
    /// <summary>Fase 3: confirmarRecebimento.</summary>
    Recebimento = 3,
    /// <summary>Venda efetivada (00RV).</summary>
    Concluida = 4,
    /// <summary>Ciclo encerrado por estorno.</summary>
    Estornada = 5
}
