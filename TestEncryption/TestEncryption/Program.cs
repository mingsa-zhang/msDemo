using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 加密服务测试程序
/// </summary>
public class EncryptionTest
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    private const string DefaultEncryptionKey = "DbManager_DefaultKey_2025_Verified_32Bytes_";
    private const string DefaultIV = "DbManager_IV_2025_Verified_16Bytes";

    public EncryptionTest()
    {
        // 使用 SHA256 生成固定密钥（简单可靠）
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(DefaultEncryptionKey));
        _iv = SHA256.HashData(Encoding.UTF8.GetBytes(DefaultIV)).Take(16).ToArray();

        Console.WriteLine($"密钥长度: {_key.Length} 字节");
        Console.WriteLine($"IV 长度: {_iv.Length} 字节");
    }

    /// <summary>
    /// 加密字符串
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            Console.WriteLine($"加密参数: Mode={aes.Mode}, Padding={aes.Padding}, KeySize={aes.KeySize}");

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
            }

            var encrypted = Convert.ToBase64String(ms.ToArray());
            Console.WriteLine($"加密后长度: {encrypted.Length}");
            return encrypted;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加密失败: {ex.Message}");
            Console.WriteLine($"异常详情: {ex}");
            throw new CryptographicException($"加密失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 解密字符串
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            Console.WriteLine($"解密输入长度: {cipherBytes.Length} 字节");

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);

            var decrypted = sr.ReadToEnd();
            Console.WriteLine($"解密后长度: {decrypted.Length}");
            return decrypted;
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"解密失败 (CryptographicException): {ex.Message}");
            Console.WriteLine($"异常详情: {ex}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解密失败 (Exception): {ex.Message}");
            Console.WriteLine($"异常详情: {ex}");
            return string.Empty;
        }
    }
}

internal class Program
{
    static void Main()
    {
        var test = new EncryptionTest();

        var testCases = new[]
        {
            "Hello World",
            "密码123!@#",
            "Unicode 测试 🚀",
            "VeryLongPasswordThatIsLongerThan32CharactersForTesting",
            "",
            "root"
        };

        foreach (var testCase in testCases)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"测试用例: \"{testCase}\"");
            Console.WriteLine(new string('=', 60));

            try
            {
                var encrypted = test.Encrypt(testCase);
                Console.WriteLine($"加密结果: {encrypted}");

                var decrypted = test.Decrypt(encrypted);
                Console.WriteLine($"解密结果: \"{decrypted}\"");

                if (decrypted == testCase)
                {
                    Console.WriteLine("✅ 测试通过");
                }
                else
                {
                    Console.WriteLine("❌ 测试失败: 解密结果与原文不匹配");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
            }
        }
    }
}
