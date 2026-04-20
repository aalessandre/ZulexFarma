using System.Security.Cryptography;
using System.Text;

namespace ZulexPharma.Domain.Helpers;

/// <summary>
/// AES-256 CBC + PKCS7 para credenciais sensíveis (ex.: senha do colaborador no Farmácia Popular).
/// A chave é derivada de uma passphrase via PBKDF2; o IV é gerado por cifragem e prefixado ao ciphertext.
/// Formato da string retornada: base64(IV(16B) || ciphertext).
/// </summary>
public static class CriptografiaHelper
{
    private const string Passphrase = "ZulexPharma.FP.v1.2026";
    private static readonly byte[] Salt = { 0x5A, 0x75, 0x6C, 0x65, 0x78, 0x50, 0x68, 0x32, 0x36, 0x46, 0x50, 0x21, 0x23, 0x34, 0x35, 0x36 };

    private static byte[] DeriveKey()
    {
        using var kdf = new Rfc2898DeriveBytes(Passphrase, Salt, 100_000, HashAlgorithmName.SHA256);
        return kdf.GetBytes(32);
    }

    public static string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return null;
        using var aes = Aes.Create();
        aes.Key = DeriveKey();
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var result = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
        return Convert.ToBase64String(result);
    }

    public static string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return null;
        var all = Convert.FromBase64String(cipherText);
        if (all.Length <= 16) return null;
        using var aes = Aes.Create();
        aes.Key = DeriveKey();
        var iv = new byte[16];
        var cipher = new byte[all.Length - 16];
        Buffer.BlockCopy(all, 0, iv, 0, 16);
        Buffer.BlockCopy(all, 16, cipher, 0, cipher.Length);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
