namespace ZulexPharma.Domain.Enums;

/// <summary>
/// Modo de operação do SNGPC na finalização da venda.
/// Parametrizado na Configuração <c>sngpc.modo</c>.
/// </summary>
public enum ModoSngpc
{
    /// <summary>
    /// Abre a modal obrigatoriamente. Bloqueia venda se algum controlado não tiver
    /// saldo de lote rastreado. Única forma 100% conforme com Anvisa.
    /// </summary>
    Obrigatorio = 0,

    /// <summary>
    /// Não abre modal. A venda é finalizada marcando <c>SngpcPendente=true</c>
    /// e os lotes são baixados por FEFO normal. O operador lança a receita depois
    /// na tela SNGPC → Lançamentos Pendentes.
    /// </summary>
    NaoLancar = 1,

    /// <summary>
    /// Abre a modal, mas permite pular com "Lançar Depois" mediante senha de supervisor
    /// com permissão adequada. Quando pulado, marca <c>SngpcPendente=true</c>.
    /// </summary>
    Misto = 2
}
