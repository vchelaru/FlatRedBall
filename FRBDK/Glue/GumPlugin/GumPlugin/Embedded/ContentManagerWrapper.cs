using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gum
{
    public class ContentManagerWrapper : RenderingLibrary.Content.IContentLoader
    {
        public string ContentManagerName
        {
            get;
            set;
        }

        public T TryLoadContent<T>(string contentName)
        {
            if (typeof(T) == typeof(RenderingLibrary.Graphics.AtlasedTexture))
            {
                var frbAtlasedTexture = Graphics.Texture.AtlasLoader.LoadAtlasedTextureByFileName(contentName);

                if (frbAtlasedTexture != null)
                {
                    RenderingLibrary.Graphics.AtlasedTexture toReturn = new RenderingLibrary.Graphics.AtlasedTexture(
                        frbAtlasedTexture.Name,
                        frbAtlasedTexture.Texture,
                        frbAtlasedTexture.SourceRectangle.ToSystemDrawing(),
                        frbAtlasedTexture.Size.ToSystemNumerics(),
                        frbAtlasedTexture.Origin.ToSystemNumerics(),
                        frbAtlasedTexture.IsRotated
                        );

                    return (T)(object)toReturn;
                }
                else
                {
                    return default(T);
                }
            }
            else
            {
                return LoadContent<T>(contentName);
            }
        }


        public T LoadContent<T>(string contentName)
        {
#if ANDROID || IOS
			contentName = contentName.ToLowerInvariant();
#endif
            return FlatRedBall.FlatRedBallServices.Load<T>(contentName, ContentManagerName);
        }
    }
}
