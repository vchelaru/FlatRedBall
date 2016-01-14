using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Performance.Measurement
{
    public class RenderBreakSave
    {
        #region Fields

        public string Texture;
        public string LayerName;

        public ColorOperation ColorOperation;
        public BlendOperation BlendOperation;
        public TextureFilter TextureFilter;
        public TextureAddressMode TextureAddressMode;

        public string Details;

        #endregion

        public static RenderBreakSave FromRenderBreak(RenderBreak renderBreak)
        {
            RenderBreakSave toReturn = new RenderBreakSave();
            if (renderBreak.Texture != null)
            {
                toReturn.Texture = renderBreak.Texture.Name;
            }

            toReturn.LayerName = renderBreak.LayerName;

            toReturn.ColorOperation = renderBreak.ColorOperation;
            toReturn.BlendOperation = renderBreak.BlendOperation;
            toReturn.TextureFilter = renderBreak.TextureFilter;
            toReturn.TextureAddressMode = renderBreak.TextureAddressMode;

#if DEBUG
            toReturn.Details = renderBreak.Details;
#endif

            return toReturn;
        }

        public static List<RenderBreakSave> FromRenderBreaks(IEnumerable<RenderBreak> renderBreaks)
        {
            List<RenderBreakSave> toReturn = new List<RenderBreakSave>();
            foreach (var renderBreak in renderBreaks)
            {
                toReturn.Add(FromRenderBreak(renderBreak));
            }

            return toReturn;
        }

        public override string ToString()
        {
            string textureName = "<NO TEXTURE>";
            if (!string.IsNullOrEmpty(Texture))
            {
                if (FileManager.IsRelative(Texture))
                {
                    textureName = Texture;
                }
                else
                {
                    textureName = FileManager.RemovePath(Texture) + " (" + Texture + ")";
                }
            }

            return textureName;
        }


    }
}
