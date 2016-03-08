using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AesEncrypter
{
    class EncryptionManager
    {
        private byte[] InitVector = new byte[16];
        private string mInitVectorAsString;
        private byte[] Key = new byte[16];
        private string mKeyAsString;

        bool IsValidKeyOrVector(string value)
        {
            return value != null && value.Length == 16;
        }

        public string EncryptionKey
        {
            get
            {
                return this.mKeyAsString;
            }
            set
            {
                if (!this.IsValidKeyOrVector(value))
                {
                    throw new Exception("Invalid encryption key: " + value);
                }
                this.mKeyAsString = value;
                this.AssignByteArrayFromString(this.mKeyAsString, this.Key);
            }
        }

        private void AssignByteArrayFromString(string value, byte[] byteArray)
        {
            for (int i = 0; i < value.Length; i++)
            {
                byte byteAtI = (byte)value[i];
                byteArray[i] = byteAtI;
            }
        }

        public string InitialVector
        {
            get
            {
                return this.mInitVectorAsString;
            }
            set
            {
                if (!this.IsValidKeyOrVector(value))
                {
                    throw new Exception("Invalid encryption key: " + value);
                }
                this.mInitVectorAsString = value;
                this.AssignByteArrayFromString(this.mInitVectorAsString, this.InitVector);
            }
        }


        public EncryptionManager()
        {
            //this.InitialVector = "123456789ABCDEF1";
            //this.EncryptionKey = "123456789ABCDEF1";
        }



        public byte[] EncryptToBytes(byte[] unencryptedBytes)
        {
            if (unencryptedBytes == null || unencryptedBytes.Length <= 0)
            {
                throw new ArgumentNullException("plainText");
            }
            using (var aesAlg = new RijndaelManaged())
            {
                aesAlg.Key = this.Key;
                aesAlg.IV = this.InitVector;
                aesAlg.Padding = PaddingMode.PKCS7;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (var msEncrypt = new MemoryStream())
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(unencryptedBytes, 0, unencryptedBytes.Length);
                    csEncrypt.FlushFinalBlock();
                    var toReturn = msEncrypt.ToArray();

                    return toReturn;
                }
            }
        }


        public byte[] DecryptFromStream(Stream stream, int encryptedByteCount)
        {
            // Create the streams used for decryption. 
            using (var csDecrypt = GetDecryptStream(stream))
            {
                byte[] toReturn = new byte[encryptedByteCount];
                csDecrypt.Read(toReturn, 0, encryptedByteCount);

                return toReturn;
            }
        }


        private CryptoStream GetDecryptStream(Stream encryptedStream)
        {
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = this.Key;
                rijAlg.IV = this.InitVector;
                rijAlg.Padding = PaddingMode.PKCS7;
                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                return new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read);
            }
        }

        public void DecryptFromStreamToStream(Stream fromStream, Stream toStream, Action<string> updateAction = null)
        {
            using (var decryptStream = GetDecryptStream(fromStream))
            {
                CopyFromStreamToStream(decryptStream, toStream, (int)fromStream.Length, updateAction);
            }
        }

        private void CopyFromStreamToStream(Stream fromStream, Stream toStream, int size, Action<string> updateAction)
        {
            byte[] buffer = new byte[1024 * 8];
            int totalRead = 0;

            while (true)
            {
                int numberRead = fromStream.Read(buffer, 0, buffer.Length);

                if (numberRead == 0)
                {
                    break;
                }
                else
                {
                    totalRead += numberRead;

                    if (updateAction != null)
                    {
                        string updateText = null;

                        float percentage = 100 * (totalRead / (float)size);
                        updateText = "Processing " + percentage.ToString("##") + "%";


                        string amountReadAsString = null;
                        if (totalRead < 1024)
                        {
                            amountReadAsString = totalRead.ToString() + " bytes";
                        }
                        else if (totalRead < 1024 * 1024)
                        {
                            amountReadAsString = ((float)totalRead / 1024).ToString("#") + "kb";
                        }
                        else
                        {
                            amountReadAsString = ((float)totalRead / (1024 * 1024)).ToString("#") + "mb";
                        }

                        if (!string.IsNullOrEmpty(updateText))
                        {
                            updateText += "\n";
                        }
                        updateText += amountReadAsString;

                        updateAction(updateText);
                    }

                    toStream.Write(buffer, 0, numberRead);
                }
            }
        }

        public byte[] DecryptFromBytes(byte[] encryptedBytes)
        {
            using (MemoryStream encryptedStream = new MemoryStream(encryptedBytes))
            {
                return DecryptFromStream(encryptedStream, encryptedBytes.Length);
            }
        }

    }
}
