namespace SwiftCare.Interfaces
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        string Encrypt(int value);
        int DecryptToInt(string encryptedValue);
    }

}
