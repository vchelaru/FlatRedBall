using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#if !XBOX360
using System.Security.Cryptography;
#endif
using System.IO;
using System.Text;

// TODO: replace this with the type you want to read.
using TRead = FlatRedBall.IO.Csv.RuntimeCsvRepresentation;
using FlatRedBall.IO.Csv;

namespace FlatRedBall.Content
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content
    /// Pipeline to read the specified data type from binary .xnb format.
    /// 
    /// Unlike the other Content Pipeline support classes, this should
    /// be a part of your main game project, and not the Content Pipeline
    /// Extension Library project.
    /// </summary>
    public class CsvReader : ContentTypeReader<TRead>
    {
        protected override TRead Read(ContentReader input, TRead existingInstance)
        {
            FlatRedBall.IO.Csv.RuntimeCsvRepresentation csv = null;
            string assetName = input.AssetName;

            //read encrypted/not-encrypted flag first
            bool isEncrypted = input.ReadBoolean();
#if !XBOX360
            if (isEncrypted)
            {
                string password = FlatRedBallServices.EncryptionKey;
                byte[] saltValue = Encoding.UTF8.GetBytes(FlatRedBallServices.EncryptionSaltValue);

                if (password == null)
                    throw new InvalidOperationException("FlatRedBallServices.EncryptionKey cannot be null, this encrypted CSV cannot be read.");

                AesManaged aes = null;
                CryptoStream cryptoStream = null;
                BinaryReader cryptoWrapper = null;

                try
                {
                    Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, saltValue);
                    aes = new AesManaged();
                    aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
                    aes.IV = deriveBytes.GetBytes(aes.BlockSize / 8);

                    cryptoStream = new CryptoStream(input.BaseStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                    cryptoWrapper = new BinaryReader(cryptoStream);
                    ReadFromStream(cryptoWrapper, assetName, out csv);
                }
                finally
                {
                    if (cryptoWrapper != null)
                        cryptoWrapper.Close();

                    if (cryptoStream != null)
                        cryptoStream.Close();

                    if (aes != null)
                        aes.Clear();
                }
            }
#elif XBOX360
            if(isEncrypted)
            {
                throw new NotSupportedException();
            }
#endif
            else
            {
                ReadFromStream(input, assetName, out csv);
            }
            
            return csv;
        }


        private void ReadFromStream(BinaryReader reader, string assetName, out RuntimeCsvRepresentation csvOutput)
        {
            csvOutput = new FlatRedBall.IO.Csv.RuntimeCsvRepresentation();

            int numberOfRows = reader.ReadInt32();

            int numberOfColumns = reader.ReadInt32();

            csvOutput.Name = assetName;

            csvOutput.Headers = new CsvHeader[numberOfColumns];

            for (int i = 0; i < numberOfColumns; i++)
            {
                csvOutput.Headers[i] = new CsvHeader(reader.ReadString());
            }

            csvOutput.Records = new List<string[]>();

            for (int row = 0; row < numberOfRows; row++)
            {
                string[] record = new string[numberOfColumns];
                csvOutput.Records.Add(record);
                for (int column = 0; column < numberOfColumns; column++)
                {
                    record[column] = reader.ReadString();
                }
            }
        }
    }
}
