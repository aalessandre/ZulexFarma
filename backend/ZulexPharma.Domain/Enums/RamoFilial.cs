namespace ZulexPharma.Domain.Enums;

/// <summary>
/// Ramo de atividade da filial. Dirige quais features/telas/campos aparecem.
/// Multi-ramo: uma conta pode ter filiais de ramos diferentes (ex.: farmácia
/// numa filial, loja de roupas em outra). Ver docs/specs/multiramo-grade.md.
/// </summary>
public enum RamoFilial
{
    Generico = 0,
    Farmacia = 1,
    Vestuario = 2,
    Hortifruti = 3,
    Mercearia = 4
}

/// <summary>
/// Mapa Ramo → features. As chaves são consumidas pelo frontend pra gatear
/// telas, tiles e campos (mesma ideia do gate de permissão). Features comuns
/// (caixa, compras, financeiro, fiscal, clientes) NÃO dependem de ramo.
/// </summary>
public static class RamoFeatures
{
    // ── Chaves de feature ──────────────────────────────────────────────
    public const string Sngpc = "sngpc";
    public const string FarmaciaPopular = "farmacia-popular";
    public const string Receita = "receita";
    public const string Substancias = "substancias";
    public const string Grade = "grade";
    public const string Pesavel = "pesavel";

    private static readonly string[] Todas =
        { Sngpc, FarmaciaPopular, Receita, Substancias, Grade, Pesavel };

    /// <summary>Features habilitadas por um ramo.</summary>
    public static IReadOnlyList<string> Para(RamoFilial ramo) => ramo switch
    {
        RamoFilial.Farmacia   => new[] { Sngpc, FarmaciaPopular, Receita, Substancias },
        RamoFilial.Vestuario  => new[] { Grade },
        RamoFilial.Hortifruti => new[] { Pesavel },
        RamoFilial.Mercearia  => new[] { Pesavel },
        _                     => Array.Empty<string>()
    };

    /// <summary>Todas as features — usado pelo usuário SISTEMA (enxerga tudo).</summary>
    public static IReadOnlyList<string> TodasAsFeatures() => Todas;
}
