using ZulexPharma.Application.DTOs.Nfe;

namespace ZulexPharma.Application.Interfaces;

public interface INfeService
{
    Task<List<NfeListDto>> ListarAsync(long? filialId = null);
    Task<NfeDetalheDto> ObterAsync(long id);
    Task<NfeListDto> CriarRascunhoAsync(NfeFormDto dto);
    Task AtualizarRascunhoAsync(long id, NfeFormDto dto);
    Task<string> ExcluirAsync(long id);
    Task<NfeEmissaoResult> EmitirAsync(long nfeId);
    Task<NfeEventoResult> CancelarAsync(long nfeId, string justificativa);
    Task<NfeEventoResult> CartaCorrecaoAsync(long nfeId, string textoCorrecao);
    Task<NfeEventoResult> InutilizarAsync(long filialId, int serie, int numInicial, int numFinal, string justificativa);
}
