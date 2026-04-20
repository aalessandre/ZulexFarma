namespace ZulexPharma.Domain.Enums;

public enum StatusFarmaciaPopular
{
    /// <summary>coSolicitacaoFarmacia gerado, ainda não enviou Fase 1.</summary>
    Iniciada = 1,
    /// <summary>Fase 1 retornou 00S (autorizada).</summary>
    PreAutorizada = 2,
    /// <summary>Fase 1 retornou 01S (parcial).</summary>
    PreAutorizadaParcial = 3,
    /// <summary>Fase 2 retornou 00A.</summary>
    Confirmada = 4,
    /// <summary>Fase 2 retornou 01A (parcial).</summary>
    ConfirmadaParcial = 5,
    /// <summary>Fase 3 retornou 00R (intermediário, ainda sem 00RV).</summary>
    Recebida = 6,
    /// <summary>Fase 3 retornou 00RV — venda efetivada (sucesso final).</summary>
    Efetivada = 7,
    /// <summary>Estorno confirmado (00E).</summary>
    Estornada = 8,
    /// <summary>Estorno parcial (01E).</summary>
    EstornadaParcial = 9,
    /// <summary>Rejeitada em alguma das fases (código de erro DATASUS).</summary>
    Rejeitada = 10,
    /// <summary>Falha de rede / timeout / erro interno.</summary>
    Erro = 11
}
