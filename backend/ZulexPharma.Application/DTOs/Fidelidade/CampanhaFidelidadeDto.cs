using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Application.DTOs.Fidelidade;

public class CampanhaFidelidadeListDto
{
    public long Id { get; set; }
    public string? Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoFidelidade Tipo { get; set; }
    public ModoContagemFidelidade ModoContagem { get; set; }
    public decimal ValorBase { get; set; }
    public int PontosGanhos { get; set; }
    public decimal PercentualCashback { get; set; }
    public FormaRetiradaPontos FormaRetirada { get; set; }
    public int DiasValidadePontos { get; set; }
    public int LimiarAlerta { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class CampanhaFidelidadeDetalheDto : CampanhaFidelidadeListDto
{
    public string? Descricao { get; set; }
    public decimal ValorPorPonto { get; set; }
    public int DiaSemana { get; set; } = 127;
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }
    public List<long> FilialIds { get; set; } = new();
    public List<long> TipoPagamentoIds { get; set; } = new();
    public List<CampanhaFidelidadeItemDto> Itens { get; set; } = new();
}

public class CampanhaFidelidadeItemDto
{
    public long? Id { get; set; }
    public long? GrupoPrincipalId { get; set; }
    public long? GrupoProdutoId { get; set; }
    public long? SubGrupoId { get; set; }
    public long? SecaoId { get; set; }
    public long? ProdutoFamiliaId { get; set; }
    public long? FabricanteId { get; set; }
    public long? ProdutoId { get; set; }
    public bool Incluir { get; set; } = true;

    /// <summary>Nome descritivo do agrupador (apenas no retorno, pra UI não precisar fazer lookup).</summary>
    public string? Descricao { get; set; }

    // Campos específicos de Cashback por item
    public decimal? ValorVendaReferencia { get; set; }
    public decimal? PercentualCashbackItem { get; set; }
    public decimal? ValorCashbackItem { get; set; }
}

public class CampanhaFidelidadeFormDto
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public TipoFidelidade Tipo { get; set; } = TipoFidelidade.Pontos;
    public ModoContagemFidelidade ModoContagem { get; set; } = ModoContagemFidelidade.PorVenda;
    public decimal ValorBase { get; set; }
    public int PontosGanhos { get; set; }
    public decimal PercentualCashback { get; set; }
    public FormaRetiradaPontos FormaRetirada { get; set; } = FormaRetiradaPontos.DescontoNaVenda;
    public decimal ValorPorPonto { get; set; }
    public int DiasValidadePontos { get; set; }
    public int LimiarAlerta { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime? DataHoraFim { get; set; }
    public int DiaSemana { get; set; } = 127;
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }
    public bool Ativo { get; set; } = true;
    public List<long> FilialIds { get; set; } = new();
    public List<long> TipoPagamentoIds { get; set; } = new();
    public List<CampanhaFidelidadeItemDto> Itens { get; set; } = new();
}
