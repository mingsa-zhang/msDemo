using System.Security.Cryptography;
using System.Text;

namespace DbManager.Common;

public static class PasswordEncryptHelper
{
    private static readonly byte[] _key = Encoding.UTF8.GetBytes("DbManager2024Key"); // 16 bytes for AES-128
    private static readonly byte[] _iv = Encoding.UTF8.GetBytes("DbManager2024IV_"); // 16 bytes

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            var bytes = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return cipherText;
        }
    }

    public static bool IsEncrypted(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        try
        {
            Convert.FromBase64String(text);
            return Decrypt(text) != text;
        }
        catch
        {
            return false;
        }
    }
}
