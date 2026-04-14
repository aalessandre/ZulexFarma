using ZulexPharma.Application.DTOs.GestorTributario;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Interface agnóstica de provedor de classificação tributária.
/// Implementações: <c>AvantGestorTributarioProvider</c> (figurafiscal),
/// <c>ImendesGestorTributarioProvider</c> (futuro), etc.
///
/// O provider é responsável por chamar a API externa, fazer parse do retorno específico
/// e devolver um <see cref="ProdutoFiscalExternoDto"/> normalizado.
/// </summary>
public interface IGestorTributarioProvider
{
    /// <summary>Identificador curto do provedor, ex: "avant".</summary>
    string Nome { get; }

    /// <summary>Limite mensal de requisições deste provedor (pra rate limit defensivo).</summary>
    int LimiteMensal { get; }

    /// <summary>Consulta dados fiscais de um único produto via EAN.</summary>
    Task<ProdutoFiscalExternoDto?> ConsultarPorEanAsync(string ean, CancellationToken ct = default);

    /// <summary>Revisão fiscal em lote. Provider faz a quebra interna conforme seu limite de batch.</summary>
    Task<ResultadoRevisaoDto> RevisarLoteAsync(List<ProdutoRevisaoDto> itens, CancellationToken ct = default);

    /// <summary>Retorna true se todas as credenciais estão preenchidas e são válidas.</summary>
    Task<bool> TestarConexaoAsync(CancellationToken ct = default);
}
