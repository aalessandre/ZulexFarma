using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.Interfaces;

public interface IEntregaService
{
    /// <summary>Preview de cálculo antes de salvar (usado na modal de confirmação do caixa).</summary>
    Task<EntregaPreviewDto> CalcularAsync(long filialId, long enderecoEntregaId);

    /// <summary>Cria a Entrega vinculada à Venda (snapshot de endereço + distância + valor).</summary>
    Task<EntregaDetalheDto> CriarAsync(EntregaFormDto dto, long? usuarioId);

    Task<List<EntregaListDto>> ListarAsync(long? filialId = null, StatusEntrega? status = null,
        DateTime? dataInicio = null, DateTime? dataFim = null);

    Task<EntregaDetalheDto> ObterAsync(long id);

    Task AtribuirEntregadorAsync(long id, long entregadorId, long? usuarioId);

    Task MudarStatusAsync(long id, StatusEntrega novoStatus, long? usuarioId, string? observacao = null);

    Task<EntregaRastreioPublicoDto> ObterPorTokenAsync(Guid tokenRastreamento);
}
