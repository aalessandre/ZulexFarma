using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.Interfaces;

public interface IEntregaService
{
    /// <summary>Preview de cálculo antes de salvar (usado na modal de confirmação do caixa).</summary>
    /// <param name="dataHora">Data/hora da entrega (resolve perfil na agenda). Default: agora (UTC).</param>
    Task<EntregaPreviewDto> CalcularAsync(long filialId, long enderecoEntregaId, DateTime? dataHora = null);

    /// <summary>Cria a Entrega vinculada à Venda (snapshot de endereço + distância + valor).</summary>
    Task<EntregaDetalheDto> CriarAsync(EntregaFormDto dto, long? usuarioId);

    Task<List<EntregaListDto>> ListarAsync(long? filialId = null, StatusEntrega? status = null,
        DateTime? dataInicio = null, DateTime? dataFim = null);

    Task<EntregaDetalheDto> ObterAsync(long id);

    Task AtribuirEntregadorAsync(long id, long entregadorId, long? usuarioId);

    Task MudarStatusAsync(long id, StatusEntrega novoStatus, long? usuarioId, string? observacao = null);

    /// <summary>Confirma entrega (Status=Entregue). Se a venda tinha PagamentoRecebido=false, contabiliza CaixaMovimentos agora no caixa atual.</summary>
    Task BaixarAsync(long id, EntregaBaixarDto dto, long? usuarioId);

    /// <summary>Cancela a entrega. Bloqueia se a venda já foi recebida (oriente a cancelar a venda).</summary>
    Task CancelarAsync(long id, long? usuarioId, string? motivo = null);

    Task<EntregaRastreioPublicoDto> ObterPorTokenAsync(Guid tokenRastreamento);
}
