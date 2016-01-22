using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using FlatRedBall.Performance.Measurement;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBallProfiler.Managers
{
    public class RenderBreakViewModel
    {
        RenderBreak renderBreak;
        RenderBreakSave renderBreakSave;

        public string LayerName
        {
            get;
            set;
        }

        public string Texture
        {
            get;
            set;
        }

        public ColorOperation ColorOperation { get; set; }
        public BlendOperation BlendOperation { get; set; }
        public TextureFilter TextureFilter { get; set; }
        public TextureAddressMode TextureAddressMode { get; set; }

        public object ObjectCausingBreak { get; set; }

        public string Details { get; set; }


        public static RenderBreakViewModel FromRenderBreak(RenderBreak renderBreak)
        {
            RenderBreakViewModel toReturn = new RenderBreakViewModel();

            toReturn.LayerName = renderBreak.LayerName;
            if (renderBreak.Texture != null)
            {
                toReturn.Texture = renderBreak.Texture.Name;
            }

            toReturn.ColorOperation = renderBreak.ColorOperation;
            toReturn.BlendOperation = renderBreak.BlendOperation;
            toReturn.TextureFilter = renderBreak.TextureFilter;
            toReturn.TextureAddressMode = renderBreak.TextureAddressMode;

#if DEBUG
            toReturn.Details = renderBreak.Details;
#endif

            toReturn.ObjectCausingBreak = renderBreak.ObjectCausingBreak;

            return toReturn;
        }

        public static RenderBreakViewModel FromRenderBreakSave(RenderBreakSave renderBreakSave)
        {
            RenderBreakViewModel toReturn = new RenderBreakViewModel();

            toReturn.LayerName = renderBreakSave.LayerName;
            toReturn.Texture = renderBreakSave.Texture;

            toReturn.ColorOperation = renderBreakSave.ColorOperation;
            toReturn.BlendOperation = renderBreakSave.BlendOperation;
            toReturn.TextureFilter = renderBreakSave.TextureFilter;
            toReturn.TextureAddressMode = renderBreakSave.TextureAddressMode;

#if DEBUG
            toReturn.Details = renderBreakSave.Details;
#endif
            return toReturn;
        }


        public override string ToString()
        {
            string toReturn = "<NO TEXTURE>";
            if (ObjectCausingBreak is FlatRedBall.Sprite)
            {
                var sprite = ObjectCausingBreak as FlatRedBall.Sprite;

                if (!string.IsNullOrEmpty(sprite.Name))
                {
                    toReturn = sprite.Name;
                }
                else
                {
                    toReturn = "Unnnamed Sprite";
                }

                if(FlatRedBall.SpriteManager.ZBufferedSprites.Contains(sprite))
                {
                    toReturn += " ZBuffered ";   
                }

                if (sprite.Texture != null)
                {
                    toReturn += " (" + sprite.Texture.Name + ")";
                }

            } 
            else if (!string.IsNullOrEmpty(Texture))
            {
                if (FileManager.IsRelative(Texture))
                {
                    return Texture;
                }
                else
                {
                    toReturn = FileManager.RemovePath(Texture) + " (" + Texture + ")";
                }
            }
            else if(ObjectCausingBreak != null && ObjectCausingBreak is IDrawableBatch)
            {
                toReturn = ObjectCausingBreak.GetType().Name + " (IDB)";
            }

            

            return toReturn;
        }
    }
}
