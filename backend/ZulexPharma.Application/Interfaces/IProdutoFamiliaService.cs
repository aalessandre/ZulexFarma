using ZulexPharma.Application.DTOs.Produtos;

namespace ZulexPharma.Application.Interfaces;

public interface IProdutoFamiliaService
{
    Task<List<ProdutoFamiliaListDto>> ListarAsync();
    Task<ProdutoFamiliaListDto> CriarAsync(ProdutoFamiliaFormDto dto);
    Task AtualizarAsync(long id, ProdutoFamiliaFormDto dto);
    Task<string> ExcluirAsync(long id);
}
