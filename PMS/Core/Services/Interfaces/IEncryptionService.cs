namespace PMS.Core.Services.Interfaces;

public interface IEncryptionService
{
    string? Encrypt(string? plainText);
    string? Decrypt(string? cipherText);
    void EncryptFile(string inputFile, string outputFile);
    void DecryptFile(string inputFile, string outputFile);
}