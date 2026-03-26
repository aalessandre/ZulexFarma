using ZulexPharma.Application.DTOs.Logs;

namespace ZulexPharma.Application.Interfaces;

public interface ILogAcaoService
{
    Task RegistrarAsync(string tela, string acao, string entidade, long registroId,
                        Dictionary<string, string?>? anterior = null,
                        Dictionary<string, string?>? novo = null);

    Task<List<LogAcaoListDto>> ListarPorRegistroAsync(string entidade, long registroId,
        DateTime? dataInicio = null, DateTime? dataFim = null);
}
