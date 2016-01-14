using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics.Texture;

#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
#endif

#if XNA4
using Color = Microsoft.Xna.Framework.Color;
#endif


namespace FlatRedBall.Graphics.Renderers
{
    public static class ImageDataRenderer
    {
        #region Fields

        static Color[] sTemporaryTextureBuffer;

        #endregion

        #region Methods

        public static void RenderSprite(Sprite sprite, int leftPixel, int topPixel, int pixelWidth, int pixelHeight, ImageData imageData)
        {
            if (sprite.Texture == null || sprite.BlendOperation != BlendOperation.Regular)
            {
                // for now throw an exception, later we may want to handle pure color rendering and stuff like that
                throw new NotImplementedException();

            }

            ImageData spriteTextureImageData = ImageData.FromTexture2D(sprite.Texture);

#if FRB_MDX
            ColorOperation colorOperation = GraphicalEnumerations.TranslateTextureOperationToColorOperation(sprite.ColorOperation);

#else
            ColorOperation colorOperation = sprite.ColorOperation;
#endif

            spriteTextureImageData.ApplyColorOperation(colorOperation, sprite.Red, sprite.Green, sprite.Blue, sprite.Alpha);


            int rightBound = System.Math.Min(imageData.Width, leftPixel + pixelWidth);
            int bottomBound = System.Math.Min(imageData.Height, topPixel + pixelHeight);

            int actualWidth = rightBound - leftPixel;
            int actualHeight = bottomBound - topPixel;



            for (int destinationX = leftPixel; destinationX < rightBound; destinationX++)
            {
                for (int destinationY = topPixel; destinationY < bottomBound; destinationY++)
                {
                    int sourcePixelX = spriteTextureImageData.Width * (destinationX - leftPixel) / pixelWidth;
                    int sourcePixelY = spriteTextureImageData.Height * (destinationY - topPixel) / pixelHeight;

                    Color sourcePixel = spriteTextureImageData.GetPixelColor(sourcePixelX, sourcePixelY);

                    if (sourcePixel.A != 255)
                    {
                        Color destinationPixel = imageData.GetPixelColor(destinationX, destinationY);
#if FRB_MDX
                        sourcePixel = Color.FromArgb(
                            System.Math.Max(sourcePixel.A, destinationPixel.A),
                            (byte)(destinationPixel.R * (255 - sourcePixel.A) / 255.0f + sourcePixel.R * (sourcePixel.A) / 255.0f),
                            (byte)(destinationPixel.G * (255 - sourcePixel.A) / 255.0f + sourcePixel.G * (sourcePixel.A) / 255.0f),
                            (byte)(destinationPixel.B * (255 - sourcePixel.A) / 255.0f + sourcePixel.B * (sourcePixel.A) / 255.0f));

                        // This is probably not accurate, but will work currently.  Eventually we may want to look at how blending is actually performed
#else
                        sourcePixel.R = (byte)(destinationPixel.R * (255 - sourcePixel.A) / 255.0f + sourcePixel.R * (sourcePixel.A) / 255.0f);
                        sourcePixel.G = (byte)(destinationPixel.G * (255 - sourcePixel.A) / 255.0f + sourcePixel.G * (sourcePixel.A) / 255.0f);
                        sourcePixel.B = (byte)(destinationPixel.B * (255 - sourcePixel.A) / 255.0f + sourcePixel.B * (sourcePixel.A) / 255.0f);

                        // This is probably not accurate, but will work currently.  Eventually we may want to look at how blending is actually performed
                        sourcePixel.A = System.Math.Max(sourcePixel.A, destinationPixel.A);
#endif
                    }
                    imageData.SetPixel(destinationX, destinationY, sourcePixel);
                }
            }
        }

        public static ImageData RenderSpriteGrid(SpriteGrid spriteGrid)
        {
            int textureWidth = (int)(.5f + spriteGrid.FurthestRightX - spriteGrid.FurthestLeftX);
            int textureHeight = (int)(.5f + spriteGrid.FurthestTopY - spriteGrid.FurthestBottomY);

            ImageData imageData = new ImageData(textureWidth, textureHeight);

            RenderSpriteGrid(spriteGrid, imageData);

            return imageData;
        }

        public static void RenderSpriteGrid(SpriteGrid spriteGrid, ImageData imageData)
        {
            throw new NotImplementedException();

            //// The SpriteGrid should be full
            //spriteGrid.FillToBounds();

            //int textureWidth = (int)(.5f + spriteGrid.FurthestRightX - spriteGrid.FurthestLeftX);
            //int textureHeight = (int)(.5f + spriteGrid.FurthestTopY - spriteGrid.FurthestBottomY);

            //if (imageData.Width != textureWidth || imageData.Height != textureHeight)
            //{
            //    throw new ArgumentException("Image data is the wrong dimensions");
            //}

            //Dictionary<string, ImageData> mCachedImageDatas = new Dictionary<string, ImageData>();

            //int pixelWidthPerSprite = (int)(.5f + (float)textureHeight / spriteGrid.VisibleSprites.Count);

            //for (int row = spriteGrid.MinDisplayedRowIndex(); row <= spriteGrid.MaxDisplayedRowIndex(); row++)
            //{
            //    for (int column = spriteGrid.MinDisplayedColIndex(); column <= spriteGrid.MaxDisplayedColIndex(); column++)
            //    {
            //        Sprite spriteAtIndex = spriteGrid.GetSpriteAtIndex(row, column);

            //        if (!mCachedImageDatas.ContainsKey(spriteAtIndex.Texture.Name))
            //        {
            //            ImageData imageDataForThisSprite = ImageData.FromTexture2D(spriteAtIndex.Texture);

            //            mCachedImageDatas.Add(spriteAtIndex.Texture.Name, imageDataForThisSprite);
            //        }

            //        ImageData imageDataToPullFrom = mCachedImageDatas[spriteAtIndex.Texture.Name];

            //        for (int pixelX = 0; pixelX < pixelWidthPerSprite; pixelX++)
            //        {
            //            for (int pixelY = 0; pixelY < pixelWidthPerSprite; pixelY++)
            //            {
            //                float textureX = (.5f + pixelX) / pixelWidthPerSprite;
            //                float textureY = (.5f + pixelY) / pixelWidthPerSprite;

            //                int xToWriteAt = (column - spriteGrid.MinDisplayedColIndex()) * pixelWidthPerSprite + pixelX;
            //                int yToWriteAt = (spriteGrid.MaxDisplayedRowIndex() - row) * pixelWidthPerSprite + pixelY;

            //                int spriteXPixelCoordinate = (int)(.5f + spriteAtIndex.LeftTextureCoordinate * spriteAtIndex.Texture.Width) + pixelX;
            //                int spriteYPixelCoordinate = (int)(.5f + spriteAtIndex.TopTextureCoordinate * spriteAtIndex.Texture.Height) + pixelY;

            //                // For now we'll just handle the errors in a naive way.  Return here if issues occur with pixel mismatchings
            //                if (spriteXPixelCoordinate >= imageDataToPullFrom.Width)
            //                {
            //                    spriteXPixelCoordinate = imageDataToPullFrom.Width - 1;
            //                }
            //                if (spriteYPixelCoordinate >= imageDataToPullFrom.Height)
            //                {
            //                    spriteYPixelCoordinate = imageDataToPullFrom.Height - 1;
            //                }


            //                imageData.SetPixel(xToWriteAt, yToWriteAt, imageDataToPullFrom.Data[spriteXPixelCoordinate + spriteYPixelCoordinate * imageDataToPullFrom.Width]);


            //            }
            //        }
            //    }
            //}
        }

        public static void RenderText(string text, BitmapFont bitmapFont, ImageData imageData)
        {
            RenderText(text, bitmapFont, imageData, 0, 0);
        }

        public static void RenderText(string text, BitmapFont bitmapFont, ImageData imageData, int x, int y)
        {
            sTemporaryTextureBuffer = new Color[bitmapFont.Texture.Width * bitmapFont.Texture.Height];

            bitmapFont.Texture.GetData<Color>(sTemporaryTextureBuffer);

            for (int i = 0; i < text.Length; i++)
            {
                RenderLetter(text[i], bitmapFont, imageData, x, y);

                x += (int)(bitmapFont.GetCharacterSpacing(text[i]) * 8);
            }
        }

        private static void RenderLetter(char letter, BitmapFont bitmapFont, ImageData imageData, int x, int y)
        {
            float tvTop = 0;
            float tvBottom = 0;
            float tuLeft = 0;
            float tuRight = 0;

            int textureWidth = bitmapFont.Texture.Width;
            int textureHeight = bitmapFont.Texture.Height;

            bitmapFont.AssignCharacterTextureCoordinates((int)letter, out tvTop, out tvBottom, out tuLeft, out tuRight);
            
            
            float characterHeight = bitmapFont.GetCharacterHeight(letter);

            int pixelLeft = (int)(tuLeft * textureWidth);
            int pixelTop = (int)(tvTop * textureHeight);

            int pixelRight = (int)(tuRight * textureWidth);
            int pixelBottom = (int)(tvBottom * textureWidth);

            float unitPerPixel = (characterHeight / (float)(pixelBottom - pixelTop));

            int pixelFromTop = (int)(bitmapFont.LineHeightInPixels * bitmapFont.DistanceFromTopOfLine((int)letter) / (2));// * .25f);


            for (int sourceY = pixelTop; sourceY < pixelBottom; sourceY++)
            {
                for (int sourceX = pixelLeft; sourceX < pixelRight; sourceX++)
                {
                    Color colorFromSource = sTemporaryTextureBuffer[sourceY * textureHeight + sourceX];

                    if (colorFromSource.A != 0)
                    {

                        imageData.SetPixel(x + (sourceX - pixelLeft), pixelFromTop + y + (sourceY - pixelTop), colorFromSource);
                    }
                }
            }

        }

        #endregion
    }
}
