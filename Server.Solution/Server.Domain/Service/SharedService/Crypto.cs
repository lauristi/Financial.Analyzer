namespace Server.Api.Domain.Infrastructure
{
    using Server.Api.Domain.Service.SharedService.Interface;
    using System.Security.Cryptography;
    using System.Text;

    namespace EncryptionLib
    {
        public class Crypto : ICrypto
        {
            //---------------------------------------------------------------------------------------------------
            // Lib de criptografia, uso modelo de chave estatica porque o uso aqui era apenas para um teste
            //---------------------------------------------------------------------------------------------------
            private static byte[] static_Key = Encoding.ASCII.GetBytes(@"qwr{@^h`h&_`50/ja9!'dcmh3!uw<&=?");

            private static byte[] static_IV = Encoding.ASCII.GetBytes(@"9/\~V).A,lY&=t2b");

            public string Encrypt(string? input)
            {
                if (input == null)
                {
                    throw new ArgumentNullException("input");
                }

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = static_Key;
                    aesAlg.IV = static_IV;

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(input);
                            }
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }

            public string Decrypt(string? input)
            {
                if (input == null)
                {
                    throw new ArgumentNullException("input");
                }

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = static_Key;
                    aesAlg.IV = static_IV;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(input)))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
    }
}