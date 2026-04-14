using ZulexPharma.Application.DTOs.Sngpc;

namespace ZulexPharma.Application.Interfaces;

public interface IVendaReceitaService
{
    /// <summary>
    /// Retorna os itens controlados de uma venda e, para cada um, a lista de lotes
    /// disponíveis em estoque na filial — usado para popular a modal SNGPC.
    /// </summary>
    Task<List<ItemControladoDto>> ListarItensControladosAsync(long vendaId);

    /// <summary>
    /// Preview dos itens controlados a partir da lista do carrinho — usado ANTES da venda
    /// ser criada no banco. Permite abrir a tela de receitas sem precisar persistir a venda.
    /// <c>VendaItemId</c> vem zerado no retorno; o frontend usa <c>ProdutoId</c> como chave
    /// temporária até o POST /vendas retornar os ids reais.
    /// </summary>
    Task<List<ItemControladoDto>> ListarItensControladosPreviewAsync(ItensControladosPreviewRequest request);

    /// <summary>
    /// Registra uma ou mais receitas para uma venda (na finalização ou retroativamente).
    /// Para cada receita, baixa o lote informado e grava o vínculo.
    /// </summary>
    Task RegistrarReceitasAsync(long vendaId, List<VendaReceitaFormDto> receitas, long? usuarioId = null);

    /// <summary>Lista vendas com SngpcPendente=true na filial.</summary>
    Task<List<VendaSngpcPendenteDto>> ListarPendentesAsync(long? filialId = null);

    /// <summary>Retorna as receitas já gravadas para uma venda.</summary>
    Task<List<VendaReceitaListDto>> ListarReceitasAsync(long vendaId);
}
