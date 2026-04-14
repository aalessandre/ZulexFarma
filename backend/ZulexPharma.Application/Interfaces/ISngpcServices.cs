using ZulexPharma.Application.DTOs.Sngpc;

namespace ZulexPharma.Application.Interfaces;

public interface IInventarioSngpcService
{
    Task<List<InventarioSngpcListDto>> ListarAsync(long? filialId = null);
    Task<InventarioSngpcDetalheDto> ObterAsync(long id);
    Task<InventarioSngpcListDto> CriarAsync(InventarioSngpcFormDto dto, long? usuarioId);
    Task AtualizarAsync(long id, InventarioSngpcFormDto dto);
    Task<int> FinalizarAsync(long id, long? usuarioId);
    Task ExcluirAsync(long id);
}

public interface IReceitaService
{
    Task<List<ReceitaListDto>> ListarAsync(long? filialId = null, DateTime? dataInicio = null, DateTime? dataFim = null);
    Task<ReceitaDetalheDto> ObterAsync(long id);
    Task<ReceitaListDto> CriarAsync(ReceitaFormDto dto);
    Task AtualizarAsync(long id, ReceitaFormDto dto);
    Task ExcluirAsync(long id);
}

public interface IPerdaService
{
    Task<List<PerdaListDto>> ListarAsync(long? filialId = null, DateTime? dataInicio = null, DateTime? dataFim = null);
    Task<PerdaListDto> CriarAsync(PerdaFormDto dto, long? usuarioId);
    Task ExcluirAsync(long id);
}

public interface IEstoqueSngpcService
{
    /// <summary>
    /// Retorna todos os lotes ativos (saldo > 0) de produtos controlados SNGPC (psicotrópicos/antimicrobianos).
    /// Ordenado por validade (FEFO).
    /// </summary>
    Task<List<EstoqueSngpcLinhaDto>> ListarAsync(long? filialId = null, bool incluirVencidos = true);
}

public interface ICompraSngpcService
{
    /// <summary>
    /// Lista compras finalizadas que envolveram produtos controlados SNGPC.
    /// Inclui tanto as que foram lançadas quanto as com opt-out (para visibilidade).
    /// </summary>
    Task<List<CompraSngpcListDto>> ListarAsync(long? filialId = null, DateTime? dataInicio = null, DateTime? dataFim = null);

    Task<CompraSngpcDetalheDto> ObterAsync(long compraId);

    /// <summary>
    /// Lança retroativamente no SNGPC uma compra que foi finalizada com opt-out.
    /// Cria ProdutoLote + MovimentoLote para cada item controlado que ainda não foi lançado.
    /// </summary>
    Task<int> LancarRetroativoAsync(long compraId, long? usuarioId);
}

public interface ISngpcMapaService
{
    Task<List<SngpcMapaListDto>> ListarAsync(long? filialId = null, int? ano = null);
    Task<SngpcMapaListDto> GerarAsync(GerarMapaSngpcRequest req, long? usuarioId);
    Task<string> ObterXmlAsync(long id);
    Task MarcarEnviadoAsync(long id, string? protocolo, long? usuarioId);
    Task ExcluirAsync(long id);
}
