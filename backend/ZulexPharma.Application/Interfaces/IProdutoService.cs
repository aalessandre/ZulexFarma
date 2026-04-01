using ZulexPharma.Application.DTOs.Produtos;

namespace ZulexPharma.Application.Interfaces;

public interface IProdutoService
{
    Task<List<ProdutoListDto>> ListarAsync(string? busca = null);
    Task<ProdutoDetalheDto> ObterAsync(long id);
    Task<ProdutoListDto> CriarAsync(ProdutoFormDto dto);
    Task AtualizarAsync(long id, ProdutoFormDto dto);
    Task<string> ExcluirAsync(long id);
}

public interface IProdutoLocalService
{
    Task<List<ProdutoLocalListDto>> ListarAsync();
    Task<ProdutoLocalListDto> CriarAsync(ProdutoLocalFormDto dto);
    Task AtualizarAsync(long id, ProdutoLocalFormDto dto);
    Task<string> ExcluirAsync(long id);
}
