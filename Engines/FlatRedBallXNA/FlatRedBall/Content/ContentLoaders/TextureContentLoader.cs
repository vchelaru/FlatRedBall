using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Texture;
using FlatRedBall.IO;
using FlatRedBall.IO.Gif;
using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FlatRedBall.Content.ContentLoaders
{
    public class TextureContentLoader : IContentLoader<Texture2D>
    {
        enum TextureLoadingStyle
        {
            ImageData,
            RenderTarget

        }

        // This can turn off generating mipmaps which can save memory and solves some rendering bugs on Samsung devices.
        public static bool CreateMipMaps = true;

        Texture2D LoadTexture2D(string assetName, string extension)
        {
            Texture2D loadedAsset = null;

            if (Renderer.Graphics == null)
            {
                throw new NullReferenceException("The Renderer's Graphics is null.  Call FlatRedBallServices.Initialize before attempting to load any textures");
            }

            switch (extension.ToLowerInvariant())
            {
                case "bmp":
                    {

                        ImageData bmp = FlatRedBall.IO.BmpLoader.GetPixelData(assetName);

                        bmp.Replace(FlatRedBallServices.GraphicsOptions.TextureLoadingColorKey, Color.Transparent);

                        Texture2D texture = bmp.ToTexture2D();
                        //new Texture2D(FlatRedBallServices.GraphicsDevice, bmp.Width, bmp.Height, 0, TextureUsage.None, SurfaceFormat.Color);
                        //texture.SetData<Color>(bmp.Data);
                        texture.Name = assetName;
                        loadedAsset = texture;
                        break;

                    }
                case "png":
                case "jpg":
                    bool useFrbPngLoader = false;

                    if (useFrbPngLoader)
                    {
#if MONOGAME || FNA
						throw new NotImplementedException();
#else
                        ImageData png = FlatRedBall.IO.PngLoader.GetPixelData(assetName);

                        Texture2D texture = png.ToTexture2D();

                        texture.Name = assetName;
                        loadedAsset = texture;
#endif
                    }
                    else
                    {
                        Texture2D texture = LoadTextureFromFile(assetName);
                        texture.Name = assetName;
                        loadedAsset = texture;
                    }

                    break;

                case "dds":
                case "dib":
                case "hdr":
                case "pfm":
                case "ppm":
                case "tga":
                    {
                        throw new NotImplementedException("The following texture format is not supported" +
                            extension + ".  We recommend using the .png format");
                        //break;
                    }
                case "gif":
                    {
#if MONOGAME
						throw new NotImplementedException();
#else
                        ImageDataList imageDataList = GifLoader.GetImageDataList(assetName);

                        int numberOfFrames = imageDataList.Count;

                        if (imageDataList.Count == 0)
                        {
                            throw new InvalidOperationException("The gif file " + assetName + " has no frames");
                        }
                        else
                        {
                            Texture2D texture2D = imageDataList[0].ToTexture2D();
                            texture2D.Name = assetName;
                            loadedAsset = texture2D;
                        }
                        break;
#endif

                    }

                //break;
                default:
                    throw new ArgumentException("FlatRedBall does not support the " + extension + " file type passed for loading a Texture2D");
                    //break;
            }
            return loadedAsset;
        }

        static ImageData mPremultipliedAlphaImageData = new ImageData(256, 256);

        private Texture2D LoadTextureFromFile(string loc)
        {
            bool canLoadNow = true;

#if ANDROID
							canLoadNow &= FlatRedBallServices.IsThreadPrimary();
#endif

            if (!canLoadNow)
            {
#if ANDROID
				AddTexturesToLoadOnPrimaryThread(loc);
#endif
                return null;
            }
            else
            {


                Texture2D file = null;

                Texture2D tempFile = null;

                // Monogame 3.8.1 introduces a parameter for processing a file as it's loaded. In my tests this 
                // does not speed up the load. It's actually slightly slower. Using SpriteBatches is the way to go.
                //    file = Texture2D.FromStream(Renderer.GraphicsDevice, titleStream, DefaultColorProcessors.PremultiplyAlpha);
                using (Stream titleStream = FileManager.GetStreamForFile(loc))
                {
                    tempFile = Texture2D.FromStream(Renderer.GraphicsDevice, titleStream);
                }
                file = MakePremultiplied(tempFile);

                tempFile.Dispose();

                return file;
            }
        }

        static SpriteBatch premultSpriteBatch = new SpriteBatch(Renderer.GraphicsDevice);
        public static Texture2D MakePremultiplied(Texture2D file)
        {

            bool useImageData = FlatRedBallServices.IsThreadPrimary() == false;

#if WINDOWS
            // Since the graphics device can get lost, we don't want to use render targets, so we'll fall
            // back to using ImageData:
            useImageData = true;
#endif

            if (useImageData)
            {
                // Victor Chelaru
                // April 22, 2015
                // The purpose of this
                // code is to support loading
                // textures from file (as opposed
                // to using the content pipeline) and
                // having them contain premultiplied alpha.
                // I believe at one point using render targets
                // may not have worked on a secondary thread. I 
                // switched the code to using render targets (not
                // image data), and ran the automated tests which do
                // background texture loading on PC and all seemed to
                // work okay. Render targets will be much faster than ImageData
                // and imagedata seems to throw errors on iOS, so I'm going to try
                // render targets and see what happens.
                lock (mPremultipliedAlphaImageData)
                {
                    mPremultipliedAlphaImageData.ExpandIfNecessary(file.Width, file.Height);

                    mPremultipliedAlphaImageData.CopyFrom(file);

                    mPremultipliedAlphaImageData.MakePremultiplied(file.Width * file.Height);

                    mPremultipliedAlphaImageData.ToTexture2D(file);
                }
                return file;
            }
            else
            {
                RenderTarget2D result = null;
                lock (Renderer.Graphics.GraphicsDevice)
                {
                    if (MathFunctions.IsPowerOfTwo(file.Width) &&
                        MathFunctions.IsPowerOfTwo(file.Height))
                    {
                        //Setup a render target to hold our final texture which will have premulitplied alpha values
                        result = new RenderTarget2D(Renderer.GraphicsDevice, file.Width, file.Height, CreateMipMaps, SurfaceFormat.Color, DepthFormat.None);
                    }
                    else
                    {
                        result = new RenderTarget2D(Renderer.GraphicsDevice, file.Width, file.Height);
                    }


                    Renderer.GraphicsDevice.SetRenderTarget(result);
                    Renderer.GraphicsDevice.Clear(Color.Black);

                    //Multiply each color by the source alpha, and write in just the color values into the final texture
                    BlendState blendColor = new BlendState();
                    blendColor.ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;

                    blendColor.AlphaDestinationBlend = Blend.Zero;
                    blendColor.ColorDestinationBlend = Blend.Zero;

                    blendColor.AlphaSourceBlend = Blend.SourceAlpha;
                    blendColor.ColorSourceBlend = Blend.SourceAlpha;

                    var position = Vector2.Zero;


#if MONOGAME
                    premultSpriteBatch.Begin(SpriteSortMode.Immediate, blendColor, samplerState: SamplerState.PointClamp);
#else
                    premultSpriteBatch.Begin(SpriteSortMode.Immediate, blendColor, SamplerState.PointClamp, null, null);
#endif
                    premultSpriteBatch.Draw(file, position, Color.White);
                    premultSpriteBatch.End();

                    //Now copy over the alpha values from the PNG source texture to the final one, without multiplying them
                    BlendState blendAlpha = new BlendState();
                    blendAlpha.ColorWriteChannels = ColorWriteChannels.Alpha;

                    blendAlpha.AlphaDestinationBlend = Blend.Zero;
                    blendAlpha.ColorDestinationBlend = Blend.Zero;

                    blendAlpha.AlphaSourceBlend = Blend.One;
                    blendAlpha.ColorSourceBlend = Blend.One;

#if MONOGAME
                    premultSpriteBatch.Begin(SpriteSortMode.Immediate, blendAlpha, samplerState: SamplerState.PointClamp);
#else
                    premultSpriteBatch.Begin(SpriteSortMode.Immediate, blendAlpha, SamplerState.PointClamp, null, null);
#endif
                    premultSpriteBatch.Draw(file, position, Color.White);
                    premultSpriteBatch.End();

                    //Release the GPU back to drawing to the screen
                    Renderer.GraphicsDevice.SetRenderTarget(null);
                }

                Renderer.ForceSetBlendOperation();
                Renderer.ForceSetColorOperation(Renderer.mLastColorOperationSet);

                return result;
            }
        }

        public Texture2D Load(string absoluteFileName)
        {
            var extension = FileManager.GetExtension(absoluteFileName);
            return LoadTexture2D(absoluteFileName, extension);
        }

        static internal void ClearPremultipliedAlphaImageData()
        {
            lock (mPremultipliedAlphaImageData)
            {
                mPremultipliedAlphaImageData.SetDataDimensions(1, 1);
            }
        }

#if ANDROID

		List<string> texturesToLoad = new List<string>();


        void AddTexturesToLoadOnPrimaryThread(string fullAssetName)
        {
            lock (texturesToLoad)
            {
                texturesToLoad.Add(fullAssetName);
            }
        }

        		
        public void ProcessTexturesWaitingToBeLoaded()
		{
			
            lock (texturesToLoad)
			{
				foreach (var assetName in texturesToLoad)
				{
					using (Stream stream = FileManager.GetStreamForFile(assetName))
					{
						var graphicsDevice = FlatRedBallServices.mGraphicsDevice;

						Texture2D texture = Texture2D.FromStream(graphicsDevice,
							stream);

						texture.Name = assetName;
					}
				}
			}
        }
#endif

    }
}
