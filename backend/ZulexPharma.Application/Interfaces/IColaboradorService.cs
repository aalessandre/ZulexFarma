using ZulexPharma.Application.DTOs.Colaboradores;

namespace ZulexPharma.Application.Interfaces;

public interface IColaboradorService
{
    Task<List<ColaboradorListDto>> ListarAsync();
    Task<ColaboradorDetalheDto> ObterAsync(long id);
    Task<ColaboradorListDto> CriarAsync(ColaboradorFormDto dto);
    Task AtualizarAsync(long id, ColaboradorFormDto dto);

    /// <summary>Retorna "excluido" se deletou fisicamente ou "desativado" se apenas inativou.</summary>
    Task<string> ExcluirAsync(long id);
}
