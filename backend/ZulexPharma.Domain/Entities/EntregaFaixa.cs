namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Faixa de preço de entrega por raio (em km) a partir da filial.
/// Ex: até 3km = R$5; até 5km = R$8; até 10km = R$15.
/// Busca: a primeira faixa cujo RaioMaxKm &gt;= distancia calculada (ordenado crescente).
/// </summary>
public class EntregaFaixa : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }

    /// <summary>Raio máximo em km que essa faixa cobre.</summary>
    public decimal RaioMaxKm { get; set; }

    /// <summary>Valor cobrado pela entrega nessa faixa.</summary>
    public decimal Valor { get; set; }

    /// <summary>Ordem de exibição (geralmente mesma ordenação de RaioMaxKm).</summary>
    public int Ordem { get; set; }
}
