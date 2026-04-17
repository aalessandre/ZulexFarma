namespace ZulexPharma.Domain.Enums;

/// <summary>Status do documento fiscal emitido para a Venda.</summary>
public enum StatusFiscal
{
    /// <summary>Venda sem emissão de documento fiscal.</summary>
    NaoEmitido = 0,
    Rascunho = 1,
    Enviado = 2,
    Autorizado = 3,
    Rejeitado = 4,
    Cancelado = 5,
    Inutilizado = 6
}
