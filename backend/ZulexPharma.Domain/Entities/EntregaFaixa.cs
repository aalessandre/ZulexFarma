namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Faixa de preço de entrega por raio (em km) dentro de um perfil.
/// Ex: perfil "NOTURNO": até 3km = R$7; até 6km = R$11; até 10km = R$16.
/// Busca: menor faixa do perfil onde RaioMaxKm &gt;= distancia (ordenado crescente).
/// </summary>
public class EntregaFaixa : BaseEntity
{
    /// <summary>FK pro perfil. Filial é derivada via Perfil.FilialId.</summary>
    public long PerfilId { get; set; }
    public EntregaPerfil? Perfil { get; set; }

    /// <summary>Raio máximo em km que essa faixa cobre.</summary>
    public decimal RaioMaxKm { get; set; }

    /// <summary>Valor cobrado pela entrega nessa faixa.</summary>
    public decimal Valor { get; set; }

    /// <summary>Ordem de exibição (geralmente mesma ordenação de RaioMaxKm).</summary>
    public int Ordem { get; set; }
}
