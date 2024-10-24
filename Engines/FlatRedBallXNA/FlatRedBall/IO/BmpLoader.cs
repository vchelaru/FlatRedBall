using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Graphics.Texture;
namespace FlatRedBall.IO
{
    #region XML Docs
    /// <summary>
    /// Class responsible for creating ImageDatas from BMP files.
    /// </summary>
    #endregion
    public class BmpLoader
    {

        private struct BMPHeader
        {
            public uint Size;
            public uint Width;
            public uint Height;
            public uint BitCount;
            public Color[] Pixels;
        }


        #region Methods
        #region Private Methods
        private static byte[] ReadFile(Stream stream)
        {
            int initialLength = 32768;
            byte[] buffer = new byte[initialLength];
            int bytesRead = 0;
            int chunk = 0;

            while ((chunk = stream.Read(buffer, 
                                        bytesRead, 
                                        buffer.Length - bytesRead)) > 0)
            {
                //update # of bytes read
                bytesRead += chunk;

                //if the buffer is full
                if (bytesRead == buffer.Length)
                {
                    //check if this is the end of the stream
                    int nextByte = stream.ReadByte();

                    //if so, return
                    if (nextByte == -1)
                    {
                        return buffer;
                    }

                    //otherwise, expand the buffer and repeat the process
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[bytesRead] = (byte)nextByte;
                    buffer = newBuffer;
                    bytesRead++;
                }
            }

            //when done, copy into an array of the propper size and return
            byte[] res = new byte[bytesRead];
            Array.Copy(buffer, res, bytesRead);
            return res;
        }

        private static byte[] LoadFile(string fileName)
        {
            Stream stream = FileManager.GetStreamForFile(fileName);
            byte[] source = ReadFile(stream);
            FileManager.Close(stream);
            return source;
        }

        private static bool CheckSignature(ref Stream file)
        {
            //2-byte .bmp signature
            byte[] signature = {66, 77};

            for (int i = 0; i < 2; ++i)
            {
                if (signature[i] != file.ReadByte())
                    return false;
            }

            return true;
        }

        private static void ParseBytes(ref Stream file, ref BMPHeader bmpHeader)
        {
            // detailed info:
            // http://www.daubnet.com/en/file-format-bmp

            //every BMP header is 54 bytes, followed by the image data
            //SIZE of the BMP: 2 bytes in (4 bytes long)
            //WIDTH of the BMP: 18 bytes in (4 bytes long)...in pixels
            //HEIGHT of the BMP: 22 bytes in (4 bytes long)...in pixels

            //PIXELDATA starts 54 bytes in, each row is 8 bytes long

            //grabs B[2,3,4,5] for the Size
            byte[] size = { (byte)file.ReadByte(), (byte)file.ReadByte(),
                            (byte)file.ReadByte(), (byte)file.ReadByte()};
            bmpHeader.Size = FormUint4(size);

            //current position is at Byte 6 => must go to 18

            // B[14,15,16,17]
            // size of header

            file.Position = 18;
            //grabs B[18,19,20,21] for the width
            byte[] width = { (byte)file.ReadByte(), (byte)file.ReadByte(),
                             (byte)file.ReadByte(), (byte)file.ReadByte()};
            bmpHeader.Width = FormUint4(width);

            //grabs B[22,23,24,25] for the height
            byte[] height = { (byte)file.ReadByte(), (byte)file.ReadByte(),
                              (byte)file.ReadByte(), (byte)file.ReadByte()};
            bmpHeader.Height = FormUint4(height);

            // B[26, 27] Planes

            //current position is at Byte 26 => must go to 28
            file.Position = 28;

            //grabs B[28,29] for the BitCount
            byte[] bitCount = { (byte)file.ReadByte(), (byte)file.ReadByte()};
            bmpHeader.BitCount = FormUint2(bitCount);

            // [30, 31, 32, 33] Compression
            // [34, 35, 36, 37] ImageSize


            //current position is at Byte 30 => must go to 54
            file.Position = 54;

            //craete the Color[] and copy it over to the bmpHeader
            BuildPixels(ref bmpHeader.Pixels, ref file, bmpHeader.BitCount, (int) bmpHeader.Width, (int) bmpHeader.Height);
        }

        private static uint FormUint4(byte[] bytes)
        {
            //takes a byte array of length 4, and converts it into a uint
            //this is use for getting the Size, Width, and Height of the BMP image

            return (uint)(bytes[0] + 
                         (bytes[1] * 256) + 
                         (bytes[2] * 256 * 256) + 
                         (bytes[3] * 256 * 256 * 256));            
        }

        private static uint FormUint2(byte[] bytes)
        {
            //takes a byte array of length 2, and converts it into a uint
            //this is use for getting the BitCount

            return (uint)((bytes[0] + 
                          (bytes[1] * 256)));

        }

        private static void BuildPixels(ref Color[] pixels, ref Stream file, uint bitCount, int pixelWidth, int pixelHeight)
        {
            int size = (int)(pixelWidth * pixelHeight);
            int pixelPosition = 0;
            pixels = new Color[size];

            switch (bitCount)
            {
                case 1:
                    {
                        #region 1 Byte = 8 Pixels
                        //padding with 0s up to a 32b boundary (This can be up to 31 zeros/pixels!) 
                        //every 1B = 8 pixel
                        //every Pixel is either white or black
                        //each pixel is 3B, but may not equal rowWidth (padding)
                        //to solve problem, rowWidth % 3 => padding
                        //each byte contains 8 bits => 8 pixels                        

                        while (file.Position < file.Length &&
                               pixelPosition < pixels.Length)
                        {
                            byte bits = (byte)file.ReadByte();
                            byte[] pVal = new byte[8];

                            pVal[0] = (byte)((bits << 0) >> 7);
                            pVal[1] = (byte)((bits << 1) >> 7);
                            pVal[2] = (byte)((bits << 2) >> 7);
                            pVal[3] = (byte)((bits << 3) >> 7);
                            pVal[4] = (byte)((bits << 4) >> 7);
                            pVal[5] = (byte)((bits << 5) >> 7);
                            pVal[6] = (byte)((bits << 6) >> 7);
                            pVal[7] = (byte)((bits << 7) >> 7);

                            //values for all of the pVals should be 1 or 0
                            for (int i = 0; i < pVal.Length; ++i)
                            { //steps the pixel[] ahead by 8

                                pixels[pixelPosition].A = 255;

                                if (pVal[i] == 0)
                                {
                                    //hi bit is not set, 0 = Black
                                    pixels[pixelPosition].R = 0;
                                    pixels[pixelPosition].G = 0;
                                    pixels[pixelPosition++].B = 0;
                                }
                                else
                                {
                                    //hi bit is set, 0 = Black, 1 = White
                                    pixels[pixelPosition].R = 255;
                                    pixels[pixelPosition].G = 255;
                                    pixels[pixelPosition++].B = 255;
                                }
                            }
                        }

                        break;
                        #endregion
                    }
                case 4:
                    {
                        #region 1 Byte = 2 Pixels
                        //every byte holds 2 pixels

                        while (file.Position < file.Length &&
                               pixelPosition < pixels.Length)
                        {
                            byte bitsRGB0x2 = (byte)file.ReadByte();
                            byte rVal1, gVal1, bVal1 = 0;
                            byte rVal2, gVal2, bVal2 = 0;

                            //get red
                            rVal1 = (byte)(bitsRGB0x2 >> 7); // RGB0 ---- => R
                            rVal2 = (byte)((bitsRGB0x2 << 4) >> 7); //---- RGB0 => R
                            //get green
                            gVal1 = (byte)((bitsRGB0x2 << 1) >> 7); //RGB0 ---- => G
                            gVal2 = (byte)((bitsRGB0x2 << 5) >> 7); //---- RGB0 => G
                            //get blue
                            bVal1 = (byte)((bitsRGB0x2 << 2) >> 7); //RGB0 ---- => B
                            bVal2 = (byte)((bitsRGB0x2 << 6) >> 7); // ----RGB0 => B

                            pixels[pixelPosition].A = 255;

                            pixels[pixelPosition].R = rVal1;
                            pixels[pixelPosition].G = gVal1;
                            pixels[pixelPosition++].B = bVal1;

                            pixels[pixelPosition].A = 255;

                            pixels[pixelPosition].R = rVal2;
                            pixels[pixelPosition].G = gVal2;
                            pixels[pixelPosition++].B = bVal2;
                        }

                        break;
                        #endregion
                    }
                case 8:
                    {

                        // Vic says - I'm not sure where this code came from, but it seems to set RGB values based on 8 bit
                        // without a color table. Not sure why but this might mislead users so we're going to throw an exception:
                        throw new NotImplementedException("This bmp is an 8-bit BMP with a color table. We currently do not support loading 8 bit BMPs. Consider using a PNG instead");

                        #region 1 Byte = 1 Pixel
                        //every byte holds 1 pixel
                        //padding each line with 0s up to a 32bit boundary will result in up to 28 0s = 7 'wasted pixels'.

                        while (file.Position < file.Length &&
                               pixelPosition < pixels.Length)
                        {
                            byte bitsRGB0 = (byte)file.ReadByte();
                            byte rVal, gVal, bVal = 0;

                            //get red
                            rVal = (byte)(bitsRGB0 >> 6); // RRYY YYYY => RR
                            //get green
                            gVal = (byte)((bitsRGB0 << 2) >> 6); //YYGG YYYY => GG
                            //get blue
                            bVal = (byte)((bitsRGB0 << 4) >> 6); //XXXX BBXX => BB

                            pixels[pixelPosition].A = 255;

                            pixels[pixelPosition].R = rVal;
                            pixels[pixelPosition].G = gVal;
                            pixels[pixelPosition++].B = bVal;
                        }

                        break;
                        #endregion
                    }
                case 16:
                    {
                        #region 2 Bytes = 1 Pixel
                        //every 2 bytes holds 1 pixel
                        //Padding each line with 0s up to a 32bit boundary will result in up to 3B of 0s = 3 'wasted pixels'.                                               
                        while (file.Position < file.Length &&
                               pixelPosition < pixels.Length)
                        {
                            byte bitsRG = (byte)file.ReadByte();
                            byte bitsB0 = (byte)file.ReadByte();
                            byte rVal, gVal, bVal = 0;

                            //get red
                            rVal = (byte)(bitsRG >> 4); // XXXX YYYY => XXXX
                            //get green
                            gVal = (byte)((bitsRG << 4) >> 4); //XXXX YYYY => YYYY
                            //get blue
                            bVal = (byte)(bitsB0 >> 4);

                            pixels[pixelPosition].A = 255;
                            pixels[pixelPosition].R = rVal;
                            pixels[pixelPosition].G = gVal;
                            pixels[pixelPosition++].B = bVal;
                        }

                        break;
                        #endregion
                    }
                case 24:
                    {
                        #region 3 Bytes = 1 Pixel
                        //no padding is necessary
                        //every 3B = 1 pixel
                        //every B is one color coord
                        //each color is a triple (R,G,B) w/ 1B each...24
                        //pixels = new Color[pixelData.Length / 3];

                        bool readBottomUp = true;

                        if (readBottomUp)
                        {
                            int bottomLeftJustifiedIndex = 0;
                            int x = bottomLeftJustifiedIndex % pixelWidth;
                            int y = pixelHeight - 1 - bottomLeftJustifiedIndex / pixelWidth;

                            pixelPosition = y * pixelWidth + x; 
                            
                            while (file.Position < file.Length &&
                                   pixelPosition > -1)
                            {
                                pixels[pixelPosition].A = 255;


                                pixels[pixelPosition].B = (byte)file.ReadByte();
                                pixels[pixelPosition].G = (byte)file.ReadByte();
                                pixels[pixelPosition].R = (byte)file.ReadByte();

                                bottomLeftJustifiedIndex++;
                                //file.ReadByte();//R, G, B, reserved (should be 0)
                                x = bottomLeftJustifiedIndex % pixelWidth;
                                y = pixelHeight - 1 - bottomLeftJustifiedIndex / pixelWidth;
                                pixelPosition = y * pixelWidth + x; 

                            }                           
                            
                            


                        }
                        else
                        {

                            
                            while (file.Position < file.Length &&
                                   pixelPosition < pixels.Length)
                            {
                                pixels[pixelPosition].A = 255;


                                pixels[pixelPosition].B = (byte)file.ReadByte();
                                pixels[pixelPosition].G = (byte)file.ReadByte();
                                pixels[pixelPosition++].R = (byte)file.ReadByte();




                            }
                        }

                        break;
                        #endregion
                    }
                default:
                    break;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// ImageData takes a fileName (string) and loads the BMP from the file. 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>a new ImageData, containing the width, height, and data of the BMP that was loaded</returns>
        public static ImageData GetPixelData(string fileName)
        {
            //Load the file into "byte stream"
            Stream sourceStream = new MemoryStream(LoadFile(fileName));

            //Check the signature to verify this is an actual .bmp image
            if (!CheckSignature(ref sourceStream))
                throw new ArgumentException(
                    String.Format("Argument Stream {0} does not contain a valid BMP file.", sourceStream));

            BMPHeader bmpHeader = new BMPHeader();
            ParseBytes(ref sourceStream, ref bmpHeader);
            FileManager.Close(sourceStream);

            return new ImageData((int)bmpHeader.Width, (int)bmpHeader.Height, bmpHeader.Pixels);
        }
        #endregion
        #endregion
    }
}
