using ZulexPharma.Application.DTOs.Caixa;

namespace ZulexPharma.Application.Interfaces;

public interface ICaixaMovimentoService
{
    Task<List<CaixaMovimentoListDto>> ListarPorCaixaAsync(long caixaId);
    Task<List<CaixaMovimentoListDto>> ListarPorVendaAsync(long vendaId);
    Task<List<CaixaMovimentoListDto>> ListarSangriasPendentesAsync(long filialId);
    Task<long> CriarSangriaAsync(SangriaFormDto dto, long usuarioId);
    Task<long> CriarSuprimentoAsync(SuprimentoFormDto dto, long usuarioId);
    Task<long> CriarRecebimentoAsync(RecebimentoFormDto dto, long usuarioId);
    Task<long> CriarPagamentoAsync(PagamentoFormDto dto, long usuarioId);
    Task<CaixaMovimentoListDto> BiparCanhotoAsync(string codigo, long usuarioId);
    Task ConfirmarSangriaConferenteAsync(long movimentoId, long usuarioId);
    Task<string> GerarCanhotoHtmlAsync(long movimentoId);
    Task<ConferenciaCaixaDto> ObterConferenciaAsync(long caixaId);
    Task ConferirCaixaAsync(long caixaId, ConferirCaixaFormDto dto, long usuarioId);
}
