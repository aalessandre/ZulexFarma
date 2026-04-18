using ZulexPharma.Application.DTOs.Municipios;
using ZulexPharma.Domain.Entities;

namespace ZulexPharma.Application.Interfaces;

public interface IMunicipioService
{
    /// <summary>Busca municípios por UF + termo (prefix no nome normalizado). Limite padrão 20.</summary>
    Task<List<MunicipioDto>> PesquisarAsync(string uf, string? termo, int limit = 20);

    /// <summary>Obtém Município por código IBGE.</summary>
    Task<Municipio?> ObterPorCodigoIbgeAsync(string codigoIbge);

    /// <summary>
    /// Resolve município por nome + UF (normalizado). Usado como fallback quando o cadastro
    /// não tem FK <c>MunicipioId</c> preenchida mas tem <c>Cidade</c>+<c>Uf</c> em texto.
    /// Retorna null se não achar match único.
    /// </summary>
    Task<Municipio?> ResolverAsync(string? nome, string? uf);
}
