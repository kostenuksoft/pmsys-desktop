using PMS.Core.Services.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace PMS.Core.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService()
        {
            using var sha256 = SHA256.Create();
            var fullKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(GenerateMachineKey()));

            _key = new byte[32];
            Array.Copy(fullKey, _key, 32);

            _iv = new byte[16];
            Array.Copy(fullKey, 16, _iv, 0, 16);
        }

        public string Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(cipherBytes);
        }

        private string GenerateMachineKey()
        {
            var macAddresses = GetMacAddresses();
            var machineKey = Environment.MachineName +
                                   Environment.ProcessorCount + 
                                   Environment.OSVersion +
                                   RuntimeInformation.OSArchitecture +
                                   RuntimeInformation.ProcessArchitecture +
                             string.Join("", macAddresses);
            return machineKey;
        }

        private static string[] GetMacAddresses()
        {
            try
            {
                return
                    NetworkInterface
                        .GetAllNetworkInterfaces()
                        .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                                     && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                                     && ni.OperationalStatus == OperationalStatus.Up)
                        .Select(ni => ni.GetPhysicalAddress().ToString())
                        .Where(addr => !string.IsNullOrEmpty(addr) && addr != "000000000000")
                        .OrderBy(addr => addr)
                        .ToArray();
            }
            catch
            {
                return [];
            }
        }

        public string Decrypt(string? cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var cipherBytes = Convert.FromBase64String(cipherText);
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        public void EncryptFile(string inputFile, string outputFile)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("Input file not found.", inputFile);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            using var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var encryptor = aes.CreateEncryptor();
            using var cryptoStream = new CryptoStream(fsOutput, encryptor, CryptoStreamMode.Write);

            fsInput.CopyTo(cryptoStream);
        }

        public void DecryptFile(string inputFile, string outputFile)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("Input file not found.", inputFile);

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                using var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                using var decryptor = aes.CreateDecryptor();
                using var cryptoStream = new CryptoStream(fsInput, decryptor, CryptoStreamMode.Read);

                cryptoStream.CopyTo(fsOutput);
            }
            catch
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
                throw;
            }
        }
    }
}