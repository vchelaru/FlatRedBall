#if NET6_0_OR_GREATER

using AsepriteDotNet.Aseprite;
using AsepriteDotNet.IO;
using AsepriteDotNet.Processors;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FlatRedBall.Content.ContentLoaders
{
    public static class AsepriteFileLoader
    {
        public static AsepriteFile Load(string absoluteFileName)
        {
            //  Load the aseprite file
            AsepriteFile aseFile;
            using (Stream titleStream = FileManager.GetStreamForFile(absoluteFileName))
            {
                string name = Path.GetFileNameWithoutExtension(absoluteFileName);
                aseFile = AsepriteDotNet.IO.AsepriteFileLoader.FromStream(name, titleStream);
            }

            return aseFile;
        }
    }
}
#endif