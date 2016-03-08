using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AesEncrypter
{
    class Program
    {



        static void Main(string[] args)
        {
            var response = CommandLineParameters.ValidateArgs(args);

            if (response.Succeeded == false)
            {
                System.Console.Error.WriteLine(response.Message);
            }
            else
            {
                var parameters = CommandLineParameters.Create(args);

                EncryptionManager encryptionManager = new EncryptionManager();

                encryptionManager.InitialVector = parameters.InitialVector;
                encryptionManager.EncryptionKey = parameters.EncryptionKey;


                using (var stream = System.IO.File.OpenRead(parameters.Source))
                {
                    if (parameters.IsDecrypting)
                    {
                        PerformDecrypt(parameters, encryptionManager, stream);
                    }
                    else
                    {
                        PerformEncrypt(parameters, encryptionManager, stream);
                    }
                }
                
            }
        }

        private static void PerformEncrypt(CommandLineParameters parameters, EncryptionManager encryptionManager, System.IO.FileStream stream)
        {
            var unencrypted = new byte[stream.Length];
            stream.Read(unencrypted, 0, unencrypted.Length);
            if(parameters.IsVerbose)
            {
                Console.WriteLine($"Encrypting {stream.Length} bytes");
            }
            var encryptedBytes = encryptionManager.EncryptToBytes(unencrypted);

            if(System.IO.File.Exists(parameters.Destination))
            {
                if (parameters.IsVerbose)
                {
                    Console.WriteLine($"Deleting {parameters.Destination}");
                }
                System.IO.File.Delete(parameters.Destination);
            }

            using (var writeStream = System.IO.File.OpenWrite(parameters.Destination))
            {
                writeStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                if (parameters.IsVerbose)
                {
                    Console.WriteLine($"Encrypted {encryptedBytes.Length} bytes");
                }
            }
        }

        private static void PerformDecrypt(CommandLineParameters parameters, EncryptionManager encryptionManager, System.IO.FileStream stream)
        {
            long amountToRead = stream.Length;

            var encrypted = new byte[amountToRead ];
            stream.Read(encrypted, 0, encrypted.Length);

            var decrypted = encryptionManager.DecryptFromBytes(encrypted);

            System.IO.File.WriteAllBytes(parameters.Destination, decrypted);
        }
    }
}
