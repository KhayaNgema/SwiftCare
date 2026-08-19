using SwiftCare.Helpers;
using SwiftCare.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
public class EncryptionService : IEncryptionService
{
    private readonly EncryptionConfiguration _config;

    public EncryptionService(EncryptionConfiguration config)
    {
        _config = config;
    }

    public string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = _config.Key;
            aes.IV = _config.Iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            throw new ArgumentException("Cipher text cannot be null or empty.", nameof(cipherText));
        }

        using (Aes aes = Aes.Create())
        {
            aes.Key = _config.Key;
            aes.IV = _config.Iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            var cipherBytes = Convert.FromBase64String(cipherText);

            using (var ms = new MemoryStream(cipherBytes))
            {
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }


    public string Encrypt(int value)
    {
        string valueAsString = value.ToString();
        return Encrypt(valueAsString);
    }

    public int DecryptToInt(string encryptedValue)
    {
        string decryptedString = Decrypt(encryptedValue);
        return int.Parse(decryptedString);
    }
}
