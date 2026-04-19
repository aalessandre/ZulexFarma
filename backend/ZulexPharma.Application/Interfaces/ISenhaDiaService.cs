namespace ZulexPharma.Application.Interfaces;

public interface ISenhaDiaService
{
    /// <summary>Gera a senha do dia atual (8 chars hex, expira à meia-noite UTC).</summary>
    string Gerar();

    /// <summary>Valida se a senha informada é a senha do dia atual.</summary>
    bool Validar(string senha);
}
