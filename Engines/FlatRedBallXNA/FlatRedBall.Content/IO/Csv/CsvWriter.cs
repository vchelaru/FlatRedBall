using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using System.Security.Cryptography;
using System.Text;
using System.IO;

// TODO: replace this with the type you want to write out.
using TWrite = FlatRedBall.Content.IO.Csv.BuildtimeCsvRepresentation;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif

namespace FlatRedBall.Content.IO.Csv
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class CsvWriter : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {
            bool writeEncrypted = value.EnableEncryption;

            //write encrypted/not-encrypted flag first
            output.Write(writeEncrypted);
#if XNA4
         
            if (writeEncrypted)
            {
                string password = value.EncryptionPassword; //decrypted with password set in FlatRedBallServices.EncryptionKey;
                if (password == null)
                    throw new InvalidOperationException("EncryptionPassword cannot be null, this CSV cannot be encrypted.");
                byte[] saltValue = Encoding.UTF8.GetBytes(FlatRedBallServices.EncryptionSaltValue);

                AesManaged aes = null;
                MemoryStream memoryStream = null;
                CryptoStream cryptoStream = null;
                BinaryWriter cryptoWrapper = null;

                try
                {
                    Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, saltValue);
                    aes = new AesManaged();
                    aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
                    aes.IV = deriveBytes.GetBytes(aes.BlockSize / 8);

                    //create a new stream, write the encrypted contents there, then copy the encrypted byte[] into the output stream
                    //doing it this way because we can't close/dispose the output stream parameter. if we wrapped it directly into the CryptoStream,
                    //we would not be able to dispose/close the CryptoStream or the BinaryWriter without closing the output stream.  Don't like leaving
                    //those things open and undisposed
                    memoryStream = new MemoryStream();
                    cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                    cryptoWrapper = new BinaryWriter(cryptoStream);
                    WriteToStream(cryptoWrapper, value);
                    cryptoStream.FlushFinalBlock();

                    byte[] encryptedBytes = memoryStream.ToArray();
                    output.Write(encryptedBytes);
                }
                finally
                {
                    if (cryptoWrapper != null)
                        cryptoWrapper.Close();

                    if (cryptoStream != null)
                        cryptoStream.Close();

                    if (memoryStream != null)
                        memoryStream.Close();

                    if (aes != null)
                        aes.Clear();
                }
            }
#else
            if(writeEncrypted)
            {
                throw new NotSupportedException();
            }
#endif
            else
            {
                WriteToStream(output, value);
            }
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.IO.Csv.RuntimeCsvRepresentation).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(CsvReader).AssemblyQualifiedName;
        }

        private void WriteToStream(BinaryWriter writer, BuildtimeCsvRepresentation csvInput)
        {
            // write the number of rows
            writer.Write(csvInput.Records.Count);

            // write the number of columns
            writer.Write(csvInput.Headers.Length);

            // write the headers
            for (int i = 0; i < csvInput.Headers.Length; i++)
            {
                writer.Write(csvInput.Headers[i].Name);

            }

            // write the records
            foreach (string[] record in csvInput.Records)
            {
                for (int i = 0; i < record.Length; i++)
                {
                    if (string.IsNullOrEmpty(record[i]))
                    {
                        writer.Write("");
                    }
                    else
                    {
                        writer.Write(record[i]);
                    }
                }
            }
        }
    }
}
