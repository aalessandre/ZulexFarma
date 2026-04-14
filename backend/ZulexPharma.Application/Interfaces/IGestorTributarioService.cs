using ZulexPharma.Application.DTOs.GestorTributario;

namespace ZulexPharma.Application.Interfaces;

/// <summary>
/// Serviço orquestrador do Gestor Tributário.
/// Expõe operações de consulta, revisão individual/em lote e gerenciamento de jobs.
/// </summary>
public interface IGestorTributarioService
{
    /// <summary>Consulta 1 EAN e retorna dados fiscais (para auto-preenchimento no form de produto).</summary>
    Task<ProdutoFiscalExternoDto?> ConsultarPorEanAsync(string ean);

    /// <summary>Revisa um produto específico (aplica dados em todos os ProdutoFiscal do produto).</summary>
    /// <returns>Dados aplicados, ou null se não encontrado.</returns>
    Task<ProdutoFiscalExternoDto?> RevisarProdutoAsync(long produtoId, long? usuarioId, bool forcar = false);

    /// <summary>Dispara um job em background para revisar a base toda (com filtros). Retorna jobId imediato.</summary>
    Task<long> IniciarRevisaoBaseAsync(RevisarBaseRequest req, long? usuarioId);

    /// <summary>Lista jobs recentes (default: últimos 50).</summary>
    Task<List<GestorTributarioJobDto>> ListarJobsAsync(int limite = 50);

    /// <summary>Retorna um job específico (para polling de progresso).</summary>
    Task<GestorTributarioJobDto?> ObterJobAsync(long jobId);

    /// <summary>Cancela um job em execução (se ainda estiver em executando).</summary>
    Task CancelarJobAsync(long jobId);

    /// <summary>Retorna status atual: configuração, uso do mês, alertas de rate limit.</summary>
    Task<GestorTributarioStatusDto> ObterStatusAsync();
}
