using ZulexPharma.Application.DTOs.Fiscal;

namespace ZulexPharma.Application.Interfaces;

public interface IAtualizacaoPrecoService
{
    Task<AbcFarmaEanResult> BuscarPorEanAsync(string ean, decimal aliquota);
    Task<AbcFarmaBaseInfo> ObterInfoBaseAsync();
    Task<UploadAbcFarmaResult> UploadBaseAsync(string conteudoJson);
    Task<ProcessarAtualizacaoResult> ProcessarAsync(ProcessarAtualizacaoRequest request);
    Task<List<AtualizacaoPrecoListDto>> ListarHistoricoAsync(long filialId);
    Task ReverterAsync(long atualizacaoPrecoId);
}
