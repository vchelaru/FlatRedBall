using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.IO;

namespace FlatRedBall.Graphics.Texture
{
    public static class TextureExtensions
    {
        public static string SourceFile(this Texture2D texture)
        {
            string extension = FileManager.GetExtension(texture.Name);

            if (extension.StartsWith("gif") && extension != "gif")
            {
                return FileManager.RemoveExtension(texture.Name) + ".gif";
            }
            else
            {
                return texture.Name;
            }
        }
    }
}
