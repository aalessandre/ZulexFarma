using ZulexPharma.Application.DTOs.Feriados;

namespace ZulexPharma.Application.Interfaces;

public interface IFeriadoService
{
    Task<List<FeriadoDto>> ListarAsync(int? ano = null, long? filialId = null);
    Task<FeriadoDto> CriarAsync(FeriadoFormDto dto);
    Task AtualizarAsync(long id, FeriadoFormDto dto);
    Task ExcluirAsync(long id);
    Task<FeriadoImportResultDto> ImportarNacionaisAsync(int ano);

    /// <summary>True se a data é feriado pra essa filial (Nacional OU Estadual da UF OU Municipal da filial).</summary>
    Task<bool> IsFeriadoAsync(DateOnly data, long filialId);
}
