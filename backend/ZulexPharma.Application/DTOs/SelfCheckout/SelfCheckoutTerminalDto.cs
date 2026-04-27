namespace ZulexPharma.Application.DTOs.SelfCheckout;

public class SelfCheckoutTerminalDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public int Numero { get; set; }
    public string? Apelido { get; set; }
    public bool Ativo { get; set; }
    public DateTime? UltimaAtividade { get; set; }
}

public class SelfCheckoutTerminalFormDto
{
    public int Numero { get; set; }
    public string? Apelido { get; set; }
    public bool Ativo { get; set; } = true;
}
