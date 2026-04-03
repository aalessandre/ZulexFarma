using ZulexPharma.Application.DTOs.Fiscal;

namespace ZulexPharma.Application.Interfaces;

public interface IAtualizacaoPrecoService
{
    Task<AbcFarmaBaseInfo> ObterInfoBaseAsync();
    Task<UploadAbcFarmaResult> UploadBaseAsync(string conteudoJson);
    Task<ProcessarAtualizacaoResult> ProcessarAsync(ProcessarAtualizacaoRequest request);
    Task<List<AtualizacaoPrecoListDto>> ListarHistoricoAsync(long filialId);
    Task ReverterAsync(long atualizacaoPrecoId);
}
