using AsepriteDotNet.Aseprite;
using AsepriteDotNet.IO;
using AsepriteDotNet.Processors;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Content.Aseprite
{
    public class AsepriteAnimationChainLoader
    {
        public static AnimationChainListSave ToAnimationChainListSave(FilePath filePath)
        {
            var asepriteFile = AsepriteFileLoader.FromFile(filePath.FullPath);
            return ToAnimationChainListSave(asepriteFile);
        }
        private static AnimationChainListSave ToAnimationChainListSave(AsepriteFile file)
        {
            var spriteSheet = SpriteSheetProcessor.Process(file);

            AnimationChainListSave list = new AnimationChainListSave();
            var aseTexture = spriteSheet.TextureAtlas.Texture;
            var width = aseTexture.Size.Width;
            var height = aseTexture.Size.Height;
            for (int i = 0; i < spriteSheet.Tags.Length; i++)
            {
                var tag = spriteSheet.Tags[i];

                var chain = new AnimationChainSave();

                for (int j = 0; j < tag.Frames.Length; j++)
                {
                    var aseFrame = tag.Frames[j];
                    AnimationFrameSave animationFrameSave = new AnimationFrameSave();


                    var bounds = spriteSheet.TextureAtlas.Regions[aseFrame.FrameIndex].Bounds;
                    animationFrameSave.TopCoordinate = bounds.Location.Y / (float)width;
                    animationFrameSave.LeftCoordinate = bounds.Location.X / (float)width;
                    animationFrameSave.BottomCoordinate = animationFrameSave.TopCoordinate + (bounds.Size.Height / (float)height);
                    animationFrameSave.RightCoordinate = animationFrameSave.LeftCoordinate + (bounds.Size.Width / (float)height);
                }

                chain.Name = tag.Name;
                list.AnimationChains.Add(chain);
            }
            return list;
        }



    }
}
