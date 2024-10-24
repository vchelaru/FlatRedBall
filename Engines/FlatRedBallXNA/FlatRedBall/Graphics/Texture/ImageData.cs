using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Point = Microsoft.Xna.Framework.Point;

using Color = Microsoft.Xna.Framework.Color;
using System.Runtime.InteropServices;

namespace FlatRedBall.Graphics.Texture
{
    public partial class ImageData
    {
        #region Fields

        private int width;
        private int height;

#if FRB_XNA
        // default to color, but could be something else when calling
        // This isn't use dI don't think...
        //SurfaceFormat surfaceFormat = SurfaceFormat.Color; 
#endif

        // if SurfaceFormat.Color, use these
        private Color[] mData;
        static Color[] mStaticData = new Color[128 * 128];

        // if SurfaceFormat.DXT3, use these
        private byte[] mByteData;

        #endregion

        #region Properties

        public int Width
        {
            get
            {
                return width;
            }
        }

        public int Height
        {
            get
            {
                return height;
            }
        }

        public Color[] Data
        {
            get
            {
                return mData;
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public ImageData(int width, int height)
            : this(width, height, new Color[width * height])
        {

        }

        public ImageData(int width, int height, Color[] data)
        {
            this.width = width;
            this.height = height;
            this.mData = data;
        }

        /// <summary>
        /// Constructs a new ImageData object of the given width and height with the argument data stored as a byte array.
        /// </summary>
        /// <param name="width">Width of the image data</param>
        /// <param name="height">Height of the image data</param>
        /// <param name="data">Data as a byte array</param>
        public ImageData(int width, int height, byte[] data)
        {
            this.width = width;
            this.height = height;
            this.mByteData = data;
        }

        #endregion

        #region Public Static Methods


        public static ImageData FromTexture2D(Texture2D texture2D)
        {
#if DEBUG
            if (texture2D.IsDisposed)
            {
                throw new Exception("The texture by the name " + texture2D.Name + " is disposed, so its data can't be accessed");
            }
#endif

            ImageData imageData = null;
            // Might need to make this FRB MDX as well.
#if FRB_XNA 

            switch (texture2D.Format)
            {
#if !XNA4 && !MONOGAME
                case SurfaceFormat.Bgr32:
                    {
                        Color[] data = new Color[texture2D.Width * texture2D.Height];
                        texture2D.GetData<Color>(data);

                        // BRG doesn't have alpha, so we'll assume an alpha of 1:
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i].A = 255;
                        }

                        imageData = new ImageData(
                            texture2D.Width, texture2D.Height, data);
                    }
                    break;
#endif
                case SurfaceFormat.Color:
                    {
                        Color[] data = new Color[texture2D.Width * texture2D.Height];
                        texture2D.GetData<Color>(data);

                        imageData = new ImageData(
                            texture2D.Width, texture2D.Height, data);
                    }
                    break;
                case SurfaceFormat.Dxt3:

                    Byte[] byteData = new byte[texture2D.Width * texture2D.Height];
                    texture2D.GetData<byte>(byteData);

                    imageData = new ImageData(texture2D.Width, texture2D.Height, byteData);

                    break;

                default:
                    throw new NotImplementedException("The format " + texture2D.Format + " isn't supported.");

                //break;
            }

#endif
            return imageData;
        }



        #endregion

        #region Public Methods

        public void ApplyColorOperation(ColorOperation colorOperation, float red, float green, float blue, float alpha)
        {
            Color appliedColor;
            // passed values from XNA will be 0-1, use the float constructor to create a color object (Justin 5/15/2012)
            appliedColor = new Color(red, green, blue, alpha);
            ApplyColorOperation(colorOperation, appliedColor);
        }

        public void ApplyColorOperation(ColorOperation colorOperation, Color appliedColor)
        {

            Color baseColor;

            switch (colorOperation)
            {

                case ColorOperation.Add:
                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            baseColor = GetPixelColor(x, y);
#if XNA4
                            baseColor.R = (byte)(System.Math.Min((baseColor.R + appliedColor.R), 255) * baseColor.A / 255);
                            baseColor.G = (byte)(System.Math.Min((baseColor.G + appliedColor.G), 255) * baseColor.A / 255);
                            baseColor.B = (byte)(System.Math.Min((baseColor.B + appliedColor.B), 255) * baseColor.A / 255);
#else
                            baseColor.R = (byte)System.Math.Min((baseColor.R + appliedColor.R), 255);
                            baseColor.G = (byte)System.Math.Min((baseColor.G + appliedColor.G), 255);
                            baseColor.B = (byte)System.Math.Min((baseColor.B + appliedColor.B), 255);
#endif
                            SetPixel(x, y, baseColor);
                        }
                    }
                    break;

                case ColorOperation.Modulate:

                    // Justin Johnson - May 15, 2012 - pre-multiply so we don't calculate every iteration (Justin 5/15/2012)
                    float red = appliedColor.R / 255f;
                    float green = appliedColor.G / 255f;
                    float blue = appliedColor.B / 255f;

                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            baseColor = GetPixelColor(x, y);
                            baseColor.R = (byte)(baseColor.R * red);
                            baseColor.G = (byte)(baseColor.G * green);
                            baseColor.B = (byte)(baseColor.B * blue);
                            SetPixel(x, y, baseColor);
                        }
                    }
                    break;

                case ColorOperation.Texture:
                    // no-op
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void Blit(Texture2D source, Rectangle sourceRectangle, Point destination)
        {
            ImageData sourceAsImageData = ImageData.FromTexture2D(source);

            Blit(sourceAsImageData, sourceRectangle, destination);
        }

        public void Blit(ImageData source, Rectangle sourceRectangle, Point destination)
        {
            for (int y = 0; y < sourceRectangle.Height; y++)
            {
                for (int x = 0; x < sourceRectangle.Width; x++)
                {
                    int sourceX = x + sourceRectangle.X;
                    int sourceY = y + sourceRectangle.Y;

                    int destinationX = x + destination.X;
                    int destinationY = y + destination.Y;

                    this.SetPixel(destinationX, destinationY, source.GetPixelColor(sourceX, sourceY));
                }
            }
        }

        public void CopyFrom(Texture2D texture2D)
        {
            texture2D.GetData<Color>(mData, 0, texture2D.Width * texture2D.Height);
        }

        public void CopyTo(ImageData destination, int xOffset, int yOffset)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    destination.mData[(y + yOffset) * destination.width + (x + xOffset)] = this.mData[y * width + x];
                }
            }
        }

        public void ExpandIfNecessary(int desiredWidth, int desiredHeight)
        {
            if (desiredWidth * desiredHeight > mData.Length)
            {
                SetDataDimensions(desiredWidth, desiredHeight);
            }
        }

        public void Fill(Color fillColor)
        {
            int length = Data.Length;
            for (int i = 0; i < length; i++)
            {
                Data[i] = fillColor;
            }
        }

        public void Fill(Color fillColor, Rectangle rectangle)
        {
            for (int y = 0; y < rectangle.Height; y++)
            {
                for (int x = 0; x < rectangle.Width; x++)
                {
                    SetPixel(rectangle.X + x, rectangle.Y + y, fillColor);
                }
            }
        }

        public void FlipHorizontal()
        {
            int halfWidth = width / 2;
            int widthMinusOne = width - 1;

            // This currently assumes Color.  Update to use DXT.
            for (int x = 0; x < halfWidth; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color temp = mData[x + y * Width];

                    mData[x + y * width] = mData[(widthMinusOne - x) + y * width];
                    mData[widthMinusOne - x + y * width] = temp;

                }
            }


        }

        public void FlipVertical()
        {
            int halfHeight = height / 2;
            int heightMinusOne = height - 1;

            // This currently assumes Color.  Update to use DXT.
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < halfHeight; y++)
                {
                    Color temp = mData[x + y * Width];

                    mData[x + y * width] = mData[x + (heightMinusOne - y) * width];
                    mData[x + (heightMinusOne - y) * width] = temp;

                }
            }
        }

        public Color GetPixelColor(int x, int y)
        {
            return this.Data[x + y * Width];
        }

        public void GetXAndY(int absoluteIndex, out int x, out int y)
        {
            y = absoluteIndex / this.Width;
            x = absoluteIndex % width;
        }

        public void RemoveColumn(int columnToRemove)
        {
            Color[] newData = new Color[width * height];

            int destinationY = 0;
            int destinationX = 0;

            int newWidth = width - 1;

            for (int y = 0; y < height; y++)
            {
                destinationX = 0;
                for (int x = 0; x < width; x++)
                {
                    if (x == columnToRemove)
                    {
                        continue;
                    }
                    newData[destinationY * newWidth + destinationX] = mData[y * width + x];

                    destinationX++;
                }

                destinationY++;
            }
            width = newWidth;

            mData = newData;
        }

        public void RemoveColumns(IList<int> columnsToRemove)
        {
            Color[] newData = new Color[width * height];

            int destinationY = 0;
            int destinationX = 0;

            int newWidth = width - columnsToRemove.Count;

            for (int y = 0; y < height; y++)
            {
                destinationX = 0;
                for (int x = 0; x < width; x++)
                {
                    if (columnsToRemove.Contains(x))
                    {
                        continue;
                    }
                    newData[destinationY * newWidth + destinationX] = mData[y * width + x];

                    destinationX++;
                }

                destinationY++;
            }
            width = newWidth;

            mData = newData;
        }

        #region XML Docs
        /// <summary>
        /// Removes the index row from the contained data.  Row 0 is the top of the texture.
        /// </summary>
        /// <param name="rowToRemove">The index of the row to remove.  Index 0 is the top row.</param>
        #endregion
        public void RemoveRow(int rowToRemove)
        {
            Color[] newData = new Color[width * height];

            int destinationY = 0;
            int destinationX = 0;

            for (int y = 0; y < height; y++)
            {
                if (y == rowToRemove)
                {
                    continue;
                }

                destinationX = 0;
                for (int x = 0; x < width; x++)
                {
                    newData[destinationY * width + destinationX] = mData[y * width + x];

                    destinationX++;
                }

                destinationY++;
            }
            height--;

            mData = newData;
        }

        public void RemoveRows(IList<int> rowsToRemove)
        {
            Color[] newData = new Color[width * height];

            int destinationY = 0;
            int destinationX = 0;

            for (int y = 0; y < height; y++)
            {
                if (rowsToRemove.Contains(y))
                {
                    continue;
                }

                destinationX = 0;
                for (int x = 0; x < width; x++)
                {
                    newData[destinationY * width + destinationX] = mData[y * width + x];

                    destinationX++;
                }

                destinationY++;
            }
            height -= rowsToRemove.Count;

            mData = newData;
        }

        public void Replace(Color oldColor, Color newColor)
        {
            for (int i = 0; i < mData.Length; i++)
            {
                if (mData[i] == oldColor)
                {
                    mData[i] = newColor;
                }
            }
        }

        public void RotateClockwise90()
        {
            Color[] newData = new Color[width * height];

            int newWidth = height;
            int newHeight = width;

            int xToPullFrom;
            int yToPullFrom;

            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    xToPullFrom = newHeight - 1 - y;
                    yToPullFrom = x;

                    newData[y * newWidth + x] =
                        mData[yToPullFrom * width + xToPullFrom];
                }
            }

            width = newWidth;
            height = newHeight;
            mData = newData;
        }

        public void SetDataDimensions(int desiredWidth, int desiredHeight)
        {
            mData = new Color[desiredHeight * desiredWidth];
            width = desiredWidth;
            height = desiredHeight;
        }

        public void SetPixel(int x, int y, Color color)
        {
            Data[y * width + x] = color;
        }

        public Microsoft.Xna.Framework.Graphics.Texture2D ToTexture2D()
        {
            return ToTexture2D(true, FlatRedBallServices.GraphicsDevice);
        }

        public Microsoft.Xna.Framework.Graphics.Texture2D ToTexture2D(bool generateMipmaps)
        {
            return ToTexture2D(generateMipmaps, FlatRedBallServices.GraphicsDevice);
        }

        public Microsoft.Xna.Framework.Graphics.Texture2D ToTexture2D(bool generateMipmaps, GraphicsDevice graphicsDevice)
        {
            int textureWidth = Width;
            int textureHeight = Height;

            if (textureWidth != Width || textureHeight != Height)
            {

                float ratioX = width / (float)textureWidth;
                float ratioY = height / (float)textureHeight;

                if (textureHeight * textureWidth > mStaticData.Length)
                {
                    mStaticData = new Color[textureHeight * textureWidth];
                }
                for (int y = 0; y < textureHeight; y++)
                {
                    for (int x = 0; x < textureWidth; x++)
                    {
                        //try
                        {
                            int sourcePixelX = (int)(x * ratioX);
                            int sourcePixelY = (int)(y * ratioY);

                            // temporary for testing
                            mStaticData[y * textureWidth + x] =
                                mData[((int)(sourcePixelY * Width) + (int)(sourcePixelX))];
                        }
                        //catch
                        //{
                        //    int m = 3;
                        //}
                    }
                }

                return ToTexture2D(mStaticData, textureWidth, textureHeight, generateMipmaps, graphicsDevice);
            }
            else
            {
                return ToTexture2D(mData, textureWidth, textureHeight, generateMipmaps, graphicsDevice);

            }
        }

        public void ToTexture2D(Texture2D textureToFill)
        {
            lock (Renderer.Graphics.GraphicsDevice)
            {
                // If it's disposed that means that the user is exiting the game, so we shouldn't
                // do anything

                if (!Renderer.Graphics.GraphicsDevice.IsDisposed)
                {
#if XNA4 && !MONOGAME
                    textureToFill.SetData<Color>(this.mData, 0, textureToFill.Width * textureToFill.Height);
#else
                textureToFill.SetData<Color>(this.mData);
                    
#endif
                }
            }
        }

        #endregion

        #region Internal Methods

        internal void MakePremultiplied()
        {
            MakePremultiplied(mData.Length);
        }

        internal void MakePremultiplied(int count)
        {
            for (int i = count - 1; i > -1; i--)
            {
                Color color = mData[i];

                float multiplier = color.A / 255.0f;

                color.R = (byte)(color.R * multiplier);
                color.B = (byte)(color.B * multiplier);
                color.G = (byte)(color.G * multiplier);

                mData[i] = color;
            }
        }

        internal static Microsoft.Xna.Framework.Graphics.Texture2D ToTexture2D(Color[] pixelData, int textureWidth, int textureHeight)
        {
            return ToTexture2D(pixelData, textureWidth, textureHeight, true, FlatRedBallServices.GraphicsDevice);
        }

        internal static Microsoft.Xna.Framework.Graphics.Texture2D ToTexture2D(Color[] pixelData, int textureWidth, int textureHeight, bool generateMipmaps, GraphicsDevice graphicsDevice)
        {
            // Justin Johnson - May 18, 2012 - Added XNA support for mipmap creation on generated textures
            int mipLevelWidth;
            int mipLevelHeight;
            int mipTotalPixels;
            int mipYCoordinate;
            int mipXCoordinate;
            int sourceXCoordinate;
            int sourceYCoordinate;
            int sourcePixelIndex;
            Color[] mipLevelData;

            Texture2D texture = new Texture2D(graphicsDevice, textureWidth, textureHeight, generateMipmaps, SurfaceFormat.Color);
            // creates texture for each mipmap level (level count defined automatically)
            if (generateMipmaps)
            {
                for (int i = 0; i < texture.LevelCount; i++)
                {
                    if (i == 0)
                    {
                        mipLevelData = pixelData;
                    }
                    else
                    {
                        // Scale previous texture to 50% size
                        // Since mipmaps are usually blended, interpolation is not necessary: point sampling only for speed
                        mipLevelWidth = textureWidth / 2;
                        mipLevelWidth = System.Math.Max(mipLevelWidth, 1);

                        mipLevelHeight = textureHeight / 2;
                        mipLevelHeight = System.Math.Max(mipLevelHeight, 1);

                        mipTotalPixels = mipLevelWidth * mipLevelHeight;
                        mipLevelData = new Color[mipTotalPixels];

                        for (int mipPixelIndex = 0; mipPixelIndex < mipTotalPixels; mipPixelIndex++)
                        {
                            mipYCoordinate = (int)System.Math.Floor(mipPixelIndex / (double)mipLevelWidth);
                            mipXCoordinate = mipPixelIndex - (mipYCoordinate * mipLevelWidth);
                            sourceYCoordinate = mipYCoordinate * 2;
                            sourceXCoordinate = mipXCoordinate * 2;
                            sourcePixelIndex = System.Math.Min(sourceYCoordinate * textureWidth + sourceXCoordinate, pixelData.Length - 1);
                            mipLevelData[mipPixelIndex] = pixelData[sourcePixelIndex];
                        }

                        pixelData = mipLevelData;
                        textureWidth = mipLevelWidth;
                        textureHeight = mipLevelHeight;
                    }
                    texture.SetData<Color>(i, null, mipLevelData, 0, mipLevelData.Length);
                }
            }
            else
            {
                texture.SetData<Color>(pixelData);
            }

            return texture;
        }

        #endregion

        #endregion



    }
}
