namespace ZulexPharma.Domain.Helpers;

public static class CpfCnpjHelper
{
    /// <summary>Remove tudo que não é dígito do CPF/CNPJ.</summary>
    public static string SomenteDigitos(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? string.Empty : new string(valor.Where(char.IsDigit).ToArray());
}
