using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics.Animation;
using System.Xml.Serialization;
using FlatRedBall.Attributes;
using FlatRedBall.Graphics.Texture;
using FlatRedBall.Graphics;

#if FRB_MDX

#elif FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Content.Scene;

#endif

namespace FlatRedBall.Content.AnimationChain
{
#if !UWP && !WINDOWS_8
    [Serializable]
#endif
    public class AnimationFrameSave : AnimationFrameSaveBase
    {

#if !FRB_MDX
        [XmlIgnore]
        [ExternalInstance]
#if !WINDOWS_PHONE && !WINDOWS_8 && !UWP
        [NonSerialized]
#endif
        internal Texture2D mTextureInstance;
#endif


        public AnimationFrameSave() { }

        public AnimationFrameSave(AnimationFrame template)
        {
            FrameLength = template.FrameLength;
            TextureName = template.TextureName;
            FlipVertical = template.FlipVertical;
            FlipHorizontal = template.FlipHorizontal;

            LeftCoordinate = template.LeftCoordinate;
            RightCoordinate = template.RightCoordinate;
            TopCoordinate = template.TopCoordinate;
            BottomCoordinate = template.BottomCoordinate;

            RelativeX = template.RelativeX;
            RelativeY = template.RelativeY;

            TextureName = template.Texture.Name;
        }


        public AnimationFrame ToAnimationFrame(string contentManagerName)
        {
            return ToAnimationFrame(contentManagerName, true);
        }

        public AnimationFrame ToAnimationFrame(string contentManagerName, bool loadTexture)
        {

            return ToAnimationFrame(contentManagerName, loadTexture, TextureCoordinateType.UV);
        }

        public AnimationFrame ToAnimationFrame(string contentManagerName, bool loadTexture, TextureCoordinateType coordinateType)
        {
            AnimationFrame frame = new AnimationFrame();

            #region Set basic variables

            frame.TextureName = TextureName;
            frame.FrameLength = FrameLength;

            if (loadTexture)
            {
#if FRB_MDX
            frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName, contentManagerName);

#else

                if (mTextureInstance != null)
                {
                    frame.Texture = mTextureInstance;
                }
                // I think we should tolarte frames with a null Texture
                else if (!string.IsNullOrEmpty(TextureName))
                {
					#if IOS || ANDROID
					frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName.ToLowerInvariant(), contentManagerName);
					#else
					frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName, contentManagerName);
					#endif
                    
                }
                //frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName, contentManagerName);
#endif
            }
            frame.FlipHorizontal = FlipHorizontal;
            frame.FlipVertical = FlipVertical;

            if (coordinateType == TextureCoordinateType.UV)
            {
                frame.LeftCoordinate = LeftCoordinate;
                frame.RightCoordinate = RightCoordinate;
                frame.TopCoordinate = TopCoordinate;
                frame.BottomCoordinate = BottomCoordinate;
            }
            else if (coordinateType == TextureCoordinateType.Pixel)
            {
                // April 16, 2015
                // Victor Chelaru
                // We used to throw this exception, but I don't know why we should, because
                // the Sprite won't show up, and the problem should be discoverable in tools
                // without a crash
                //if (frame.Texture == null)
                //{
                //    throw new Exception("The frame must have its texture loaded to use the Pixel coordinate type");
                //}

                if (frame.Texture != null)
                {
                    frame.LeftCoordinate = LeftCoordinate / frame.Texture.Width;
                    frame.RightCoordinate = RightCoordinate / frame.Texture.Width;

                    frame.TopCoordinate = TopCoordinate / frame.Texture.Height;
                    frame.BottomCoordinate = BottomCoordinate / frame.Texture.Height;
                }
            }
            
            
            frame.RelativeX = RelativeX;
            frame.RelativeY = RelativeY;

            #endregion

            return frame;
        }

        //public AnimationFrame ToAnimationFrame(TextureAtlas textureAtlas)
        //{
        //    AnimationFrame toReturn = ToAnimationFrame(null, false);
        //    var entry = textureAtlas.GetEntryFor(this.TextureName);

        //    if (entry != null)
        //    {

        //        float left;
        //        float right;
        //        float top;
        //        float bottom;


        //        entry.FullToReduced(toReturn.LeftCoordinate, toReturn.RightCoordinate,
        //            toReturn.TopCoordinate, toReturn.BottomCoordinate,
        //            out left, out right, out top, out bottom);

        //        toReturn.LeftCoordinate = left;
        //        toReturn.RightCoordinate = right;
        //        toReturn.TopCoordinate = top;
        //        toReturn.BottomCoordinate = bottom;

        //    }

        //    return toReturn;
        //}


        internal static AnimationFrameSave FromXElement(System.Xml.Linq.XElement element)
        {
            AnimationFrameSave toReturn = new AnimationFrameSave();

                        
            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "FlipHorizontal":
                        toReturn.FlipHorizontal = SceneSave.AsBool(subElement);
                        break;
                    case "FlipVertical":
                        toReturn.FlipVertical = SceneSave.AsBool(subElement);
                        break;
                    case "TextureName":
                        toReturn.TextureName = subElement.Value;
                        break;
                    case "FrameLength":
                        toReturn.FrameLength = SceneSave.AsFloat(subElement);
                        break;
                    case "LeftCoordinate":
                        toReturn.LeftCoordinate = SceneSave.AsFloat(subElement);
                        break;
                    case "RightCoordinate":
                        toReturn.RightCoordinate = SceneSave.AsFloat(subElement);
                        break;
                    case "TopCoordinate":
                        toReturn.TopCoordinate = SceneSave.AsFloat(subElement);
                        break;
                    case "BottomCoordinate":
                        toReturn.BottomCoordinate = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeX":
                        toReturn.RelativeX = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeY":
                        toReturn.RelativeY = SceneSave.AsFloat(subElement);
                        break;
                }
            }

            return toReturn;
        }
    }
}
