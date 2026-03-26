using ZulexPharma.Application.DTOs.Fornecedores;

namespace ZulexPharma.Application.Interfaces;

public interface IFornecedorService
{
    Task<List<FornecedorListDto>> ListarAsync();
    Task<FornecedorDetalheDto> ObterAsync(long id);
    Task<FornecedorListDto> CriarAsync(FornecedorFormDto dto);
    Task AtualizarAsync(long id, FornecedorFormDto dto);

    /// <summary>Retorna "excluido" se deletou fisicamente ou "desativado" se apenas inativou.</summary>
    Task<string> ExcluirAsync(long id);
}
