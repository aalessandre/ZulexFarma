using ZulexPharma.Application.DTOs.Grade;

namespace ZulexPharma.Application.Interfaces;

public interface IProdutoGradeService
{
    /// <summary>Estado atual da grade do produto (eixos + variações com estoque/preço da filial atual).</summary>
    Task<ProdutoGradeDto> ObterAsync(long produtoId);

    /// <summary>Liga/desliga a grade, sincroniza eixos e variações (SKUs) + estoque/preço por SKU na filial atual.</summary>
    Task SalvarAsync(long produtoId, SalvarGradeDto dto);
}
