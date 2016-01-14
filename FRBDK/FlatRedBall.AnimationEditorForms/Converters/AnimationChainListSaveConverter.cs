using FlatRedBall.Content.AnimationChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.AnimationEditorForms.Converters
{
    public static class AnimationChainListSaveConverter
    {
        public static void ConvertToPixelCoordinates(this AnimationChainListSave animationList)
        {
            if (animationList.CoordinateType == Graphics.TextureCoordinateType.Pixel)
            {
                throw new Exception("The animation is already using pixel coordinates");
            }

            animationList.CoordinateType = Graphics.TextureCoordinateType.Pixel;

            foreach (AnimationChainSave animation in animationList.AnimationChains)
            {
                foreach (var frame in animation.Frames)
                {
                    var texture = WireframeManager.Self.GetTextureForFrame(frame);

                    if (texture != null)
                    {

                        frame.LeftCoordinate *= texture.Width;
                        frame.RightCoordinate *= texture.Width;

                        frame.TopCoordinate *= texture.Height;
                        frame.BottomCoordinate *= texture.Height;


                        frame.LeftCoordinate = Math.MathFunctions.RoundToInt(frame.LeftCoordinate);
                        frame.RightCoordinate = Math.MathFunctions.RoundToInt(frame.RightCoordinate);

                        frame.TopCoordinate = Math.MathFunctions.RoundToInt(frame.TopCoordinate);
                        frame.BottomCoordinate = Math.MathFunctions.RoundToInt(frame.BottomCoordinate);
                    }

                }
            }

        }


        public static void ConvertToUvCoordinates(this AnimationChainListSave animationList)
        {
            if (animationList.CoordinateType == Graphics.TextureCoordinateType.UV)
            {
                throw new Exception("The animation is already using pixel coordinates");
            }

            animationList.CoordinateType = Graphics.TextureCoordinateType.UV;

            foreach (AnimationChainSave animation in animationList.AnimationChains)
            {
                foreach (var frame in animation.Frames)
                {
                    var texture = WireframeManager.Self.GetTextureForFrame(frame);

                    if (texture != null)
                    {
                        frame.LeftCoordinate /= texture.Width;
                        frame.RightCoordinate /= texture.Width;

                        frame.TopCoordinate /= texture.Height;
                        frame.BottomCoordinate /= texture.Height;
                    }

                }
            }
        }


    }
}
