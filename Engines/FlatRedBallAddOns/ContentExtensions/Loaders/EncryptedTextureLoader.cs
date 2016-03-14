using FlatRedBall.ContentExtensions.Encryption;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FlatRedBall.ContentExtensions.Loaders
{
    public class EncryptedTextureLoader
    {
        public Texture2D LoadEncryptedPng(string aesEncryptedFileName, string aesKey, string aesInitialVector, GraphicsDevice graphicsDevice)
        {
            var manager = new AesEncryptionManager();
            manager.EncryptionKey = aesKey;
            manager.InitialVector = aesInitialVector;

            using (var encryptedStream = System.IO.File.OpenRead(aesEncryptedFileName))
            {
                int decryptedSize;
                var decryptedBytes = manager.DecryptFromStream(encryptedStream, (int)encryptedStream.Length,
                    out decryptedSize);

                var stream = new MemoryStream(decryptedBytes, 0, decryptedSize);

                Texture2D texture2D = Texture2D.FromStream(graphicsDevice, stream);

                // This is not premult - so whoever calls this may need to convert it to premult
                return texture2D;
            }

        }
    }
}
