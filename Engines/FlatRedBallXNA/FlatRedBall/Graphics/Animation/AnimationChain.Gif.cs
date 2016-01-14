using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO.Gif;
using FlatRedBall.IO;
#if !FRB_MDX
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.Graphics.Animation
{
    public partial class AnimationChain
    {
        public static AnimationChain FromGif(string fileName, string contentManagerName)
        {
            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.RelativeDirectory + fileName;
            }

            if (FlatRedBallServices.IsLoaded<AnimationChain>(fileName, contentManagerName))
            {
                return FlatRedBallServices.GetNonDisposable<AnimationChain>(fileName, contentManagerName).Clone();
            }

            ImageDataList imageDataList = GifLoader.GetImageDataList(fileName);

            int numberOfFrames = imageDataList.Count;

            AnimationChain animationChain = new AnimationChain(numberOfFrames);

            for (int i = 0; i < numberOfFrames; i++)
            {
                // We assume GIFs are for 2D games that don't need mipmaps.  Could change this later
                // if needed
                const bool generateMipmaps = false;

                Texture2D texture2D = imageDataList[i].ToTexture2D(generateMipmaps, FlatRedBallServices.GraphicsDevice);
                texture2D.Name = 
                    fileName + i.ToString();

                if (i >= imageDataList.FrameTimes.Count)
                {
                    const double defaultFrameTime = .1;

                    animationChain.Add(
                        new AnimationFrame(
                            texture2D, (float)defaultFrameTime));
                }
                else
                {

                    animationChain.Add(
                        new AnimationFrame(
                            texture2D, (float)imageDataList.FrameTimes[i]));
                }
                FlatRedBallServices.AddDisposable(texture2D.Name, texture2D, contentManagerName);
            }

            // Don't dispose the anything because it's part of the
            // content manager.

            animationChain.ParentGifFileName = fileName;
            animationChain.Name = FileManager.RemovePath(fileName);

            return animationChain;
        }




    }
}
