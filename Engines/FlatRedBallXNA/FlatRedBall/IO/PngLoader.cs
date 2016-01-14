using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using FlatRedBall.Graphics.Texture;
namespace FlatRedBall.IO
{
    /// <summary>
    /// Notes:  Interlaced pngs are now fully supported 
    ///         Only the chunks of data that are outlined in the Png specification as Critical Chunks are supported.
    /// 
    /// TODO:   Allow use of the CRC values to check the validity of the Chunk data
    ///         Consider implementing the Ancillary Chunks since chunks like gAMA can drastically change visible colors.
    /// </summary>
    public class PngLoader
    {
        public static int ByteBufferSize = 4096;
        public static int ColorBufferSize = 512*512;

        static byte[] sByteBuffer;
        internal static Color[] mStaticPixels;
        private static byte[] mTransparentEntries;
        private static int mMaxTransIndex = -1;

        #region Constructor

        public static void Initialize()
        {
            if (sByteBuffer == null || sByteBuffer.Length < ByteBufferSize)
            {
                sByteBuffer = new byte[ByteBufferSize];
            }

            if(mStaticPixels == null || mStaticPixels.Length < ColorBufferSize)
            {
                mStaticPixels = new Color[ColorBufferSize];
            }

            if (mTransparentEntries == null || mTransparentEntries.Length < ByteBufferSize)
            {
                mTransparentEntries = new byte[ByteBufferSize];
            }
        }

        #endregion

        #region - Private Structs/Classes...

        /// <summary>
        /// Binds common .png chunk types to their Hexadecimal values.
        /// </summary>
        private enum ChunkTypes : uint
        {
            UNSUPPORTED = 0,
            IHDR = 0x49484452,
            PLTE = 0x504c5445,
            IDAT = 0x49444154,
            IEND = 0x49454e44,
            tRNS = 0x74524e53
        }


        /// <summary>
        /// Represents a typed chunk of data from the .png image.
        /// </summary>
        private struct Chunk
        {

            public uint Length;
            public ChunkTypes Type;
            public MemoryStream ChunkData;
            public uint CRC;
        }

        /// <summary>
        /// Holds all of the information contained in the IHDR chunk of the .png image.
        /// </summary>
        private struct HeaderInfo
        {
            public uint Width;                  //The width of the final image
            public uint Height;                 //The height of the final image
            public byte BitDepth;               //The number of bits-per-sample for this image
            public byte ColorType;              //Describes the way colors are represented in this image
            public byte CompressionMethod;      //Currently only 0 (zip compression) is available
            public byte FilterMethod;           //Currently only 0 is available
            public byte InterlaceMethod;        //0 - no interlace, or 1 - Adam7 interlace
        }

        #endregion

        #region - Helpers...
        /// <summary>
        /// Arranges and combines the byte values stored in byte[] bytes in order to form
        /// a uint.
        /// </summary>
        /// <param name="bytes">A byte[] of length 4 to be formed into a uint.</param>
        /// <returns>The uint represented by byte[] bytes.</returns>
        private static uint FormUint(ref Stream bytes)
        {
            return (uint)((bytes.ReadByte() << 24) |
                    (bytes.ReadByte() << 16) |
                    (bytes.ReadByte() << 8) |
                    (bytes.ReadByte()));
        }

        /// <summary>
        /// Uses the ColorType and Bitdepth of this image to calculate the number of bytes per pixel.
        /// </summary>
        /// <param name="header">The HeaderInfo containing IHDR data for this image</param>
        /// <returns>The number of BytesPerPixel in this image.</returns>
        private static float GetBytesPerPixel(HeaderInfo header)
        {
            switch ((int)(header.ColorType))
            {
                case 0: return ((float)(header.BitDepth)/ 8); //break;
                case 2: return (3 * (float)(header.BitDepth )/ 8); //break;
                case 3: return (float)(header.BitDepth) / 8; //break;
                case 4: return (2 * (float)(header.BitDepth) / 8); //break;
                case 6: return (4 * ((float)(header.BitDepth) / 8)); //break;
                default: return -1;
            }
        }

        #endregion

        #region - File Handling...
        /// <summary>
        /// Reads the data in the given Stream and stores it in a byte[] that is then trimmed and returned.
        /// </summary>
        /// <param name="stream">A read-enabled Stream that is reading from a .png file.</param>
        /// <returns>A byte[] containing the file being read through Stream stream.</returns>
        private static int ReadFile(Stream stream)
        {
            int read = 0;
            int chunk;

            //While there's something to read, read into the buffer.
            while ((chunk = stream.Read(sByteBuffer, read, sByteBuffer.Length - read)) > 0)
            {
                //update # of bytes read
                read += chunk;

                //if the buffer is full
                if (read == sByteBuffer.Length)
                {
                    //check if this is the end of the stream
                    int nextByte = stream.ReadByte();

                    //if so, return
                    if (nextByte == -1)
                    {
                        return read;
                    }

                    //otherwise, expand the buffer and repeat the process.
                    byte[] newBuffer = new byte[sByteBuffer.Length * 2];
                    Array.Copy(sByteBuffer, newBuffer, sByteBuffer.Length);
                    newBuffer[read] = (byte)nextByte;
                    sByteBuffer = newBuffer;
                    read++;
                }
            }

            //When done, copy into an array of the proper size and return.
            return read;
        }

        /// <summary>
        /// Loads a file (hopefully a .png image) into a byte[].
        /// </summary>
        /// <param name="fileName">The name of the .png file to be loaded</param>
        /// <returns>A Byte[] containing the file referred to by fileName.</returns>
        private static int LoadFile(string fileName)
        {
#if XNA4
            throw new NotImplementedException();
#else
            //Open a FileStream to the file at fileName and pass it to ReadFile.\
            System.Diagnostics.Trace.Write(Path.Combine(Microsoft.Xna.Framework.Storage.StorageContainer.TitleLocation, fileName.Replace("/", "\\")));

            FileStream file = File.OpenRead(Path.Combine(Microsoft.Xna.Framework.Storage.StorageContainer.TitleLocation, fileName.Replace("/", "\\")));
            int bytesRead = ReadFile(file);

            //Close the stream and return
           // file.Close();
            return bytesRead;
#endif
        }

        #endregion

        #region - Filtering...
        /// <summary>
        /// Reverses the filtering done on a scanline of pixels that have been filtered using Method 0 (Subtract)
        /// </summary>
        /// <param name="image">A reference to the byte[] containing the image(decompressed)</param>
        /// <param name="pointer">The index of the byte where this scanline begins(after the filter byte)</param>
        /// <param name="bps">The number of bits per scanline.</param>
        /// <param name="bpp">The number of bytes per pixel</param>
        /// <param name="bitDepth">The number of bits per sample</param>
        private static void ReverseFilterSub(ref byte[] image, uint pointer, uint bps, float bpp, byte bitDepth)
        {
            if (bitDepth >= 8)
            {
                for (uint i = (uint)(pointer + bpp); i < pointer + bps; ++i)
                {
                    //Add the byte to the corresponding byte in the previous pixel
                    image[i] += image[(int)(i - bpp)];
                }
            }
        }

        /// <summary>
        /// Reverses the filtering done on a scanline of pixels that have been filtered using Method 1 (Up)
        /// </summary>
        /// <param name="image">A reference to the byte[] containing the image(decompressed)</param>
        /// <param name="pointer">The index of the byte where this scanline begins(after the filter byte)</param>
        /// <param name="bps">The number of bits per scanline.</param>
        /// <param name="bpp">The number of bytes per pixel</param>
        /// <param name="bitDepth">The number of bits per sample</param>
        private static void ReverseFilterUp(ref byte[] image, uint pointer, uint bps, float bpp, byte bitDepth)
        {
            if (pointer < bps + 1) return;

            if (bitDepth >= 8)
            {
                for (uint i = pointer; i < pointer + bps; ++i)
                {
                    //Add byte to the byte directly above it.
                    image[i] += image[i - (bps + 1)];
                }
            }
        }

        /// <summary>
        /// Reverses the filtering done on a scanline of pixels that have been filtered using Method 2 (Average)
        /// </summary>
        /// <param name="image">A reference to the byte[] containing the image(decompressed)</param>
        /// <param name="pointer">The index of the byte where this scanline begins(after the filter byte)</param>
        /// <param name="bps">The number of bits per scanline.</param>
        /// <param name="bpp">The number of bytes per pixel</param>
        /// <param name="bitDepth">The number of bits per sample</param>
        private static void ReverseFilterAverage(ref byte[] image, uint pointer, uint bps, float bpp, byte bitDepth)
        {
            byte up;
            byte left = 0;
            bool isFirstScanline = false;

            if (pointer < bps + 1) isFirstScanline = true;
            if (isFirstScanline) up = 0;
            else up = image[pointer - (bps + 1)];

            if (bitDepth >= 8)
            {
                for (uint i = 0; i < bps; ++i)
                {
                    image[i + pointer] += (byte)((up + left) / 2);
                    if ((i + 1) >= bpp) left = image[(int)(i + pointer - bpp + 1)];
                    if (!isFirstScanline)
                        up = image[i + pointer - bps];
                }
            }
        }

        /// <summary>
        /// Reverses the filtering done on a scanline of pixels that have been filtered using Method 3 (Paeth)
        /// </summary>
        /// <param name="image">A reference to the byte[] containing the image(decompressed)</param>
        /// <param name="pointer">The index of the byte where this scanline begins(after the filter byte)</param>
        /// <param name="bps">The number of bits per scanline.</param>
        /// <param name="bpp">The number of bytes per pixel</param>
        /// <param name="bitDepth">The number of bits per sample</param>
        private static void ReverseFilterPaeth(ref byte[] image, uint pointer, uint bps, float bpp, byte bitDepth)
        {
            byte left = 0;
            byte up;
            byte upLeft = 0;
            bool isFirstScanline = false;

            if (pointer < bps + 1) isFirstScanline = true;
            if (isFirstScanline) up = 0;
            else up = image[pointer - (bps + 1)];

            if (bitDepth >= 8)
            {
                for (uint i = 0; i < bps; ++i)
                {
                    image[i + pointer] += PaethPredictor((int)left, (int)up, (int)upLeft);

                    if (i + 1 >= bpp)
                    {
                        left = image[(int)(i + pointer - bpp + 1)];
                        if (!isFirstScanline)
                            upLeft = image[(int)(i + pointer - bpp - bps)];
                    }
                    if (!isFirstScanline)
                        up = image[i + pointer - bps];

                }

            }
        }

        /// <summary>
        /// A helper for the ReverseFilterPaeth method
        /// </summary>
        /// <param name="left"></param>
        /// <param name="up"></param>
        /// <param name="upLeft"></param>
        /// <returns></returns>
        private static byte PaethPredictor(int left, int up, int upLeft)
        {
            int p = (int)(left + up - upLeft);
            int pleft = System.Math.Abs((int)(p - left));
            int pup = System.Math.Abs((int)(p - up));
            int pupleft = System.Math.Abs((int)(p - upLeft));

            if ((pleft <= pup) && (pleft <= pupleft)) return (byte)left;
            else if (pup <= pupleft) return (byte)up;
            else return (byte)upLeft;
        }

        /// <summary>
        /// Calls the appropriate Reverse Filtering methods to return the image to its original state.
        /// </summary>
        /// <param name="image">The byte[] containing the filtered(but decompressed) image.</param>
        /// <param name="header">A HeaderInfo struct containing IHDR data for this image.</param>
        /// <returns>A byte[] containing the restored, original image(in bytes)</returns>
        private static byte[] ReverseFiltering(byte[] image, HeaderInfo header)
        {
            float bpp;
            uint bps;
            uint pointer;

            #region Non-interlaced Images
            //For non-interlaced images:
            if (header.InterlaceMethod == 0)
            {
                bpp = GetBytesPerPixel(header);
                bps = (uint)System.Math.Ceiling(bpp * header.Width);
                // System.Diagnostics.Trace.WriteLine(bps);
                pointer = 0;


                for (int i = 0; i < header.Height; ++i)
                {
                    //switch on the filter byte of each scanline
                    switch ((int)image[i * (bps + 1)])
                    {
                        //call the appropriate ReverseFilter method, making sure to point to the next byte
                        case 0: break;
                        case 1: ReverseFilterSub(ref image, pointer + 1, bps, bpp, header.BitDepth); break;
                        case 2: ReverseFilterUp(ref image, pointer + 1, bps, bpp, header.BitDepth); break;
                        case 3: ReverseFilterAverage(ref image, pointer + 1, bps, bpp, header.BitDepth); break;
                        case 4: ReverseFilterPaeth(ref image, pointer + 1, bps, bpp, header.BitDepth); break;
                    }

                    //increase the pointer by 1 full scanline(including 1 for the filter byte)
                    pointer += bps + 1;
                }

                return image;
            }
            #endregion

            #region Interlaced Images
            //For interlaced Images:
            else
            {
#if PROFILE
                
#endif
                bpp = GetBytesPerPixel(header); 
                byte[] fullImage = new byte[(int)(header.Width * header.Height * bpp) + header.Height];
                byte[][] passes = new byte[7][];
                uint width = 0;
                uint height = 0;

                int offX = 0, offY = 0;
                int dX = 0, dY = 0;

                for (int i = 0; i < 7; ++i)
                {

                    //For each pass, allocate a byte[] large enough to store the bytes for that pass + the filter byte for each scanline
                    //Also specify the offsets and increment values so the actual pixels can be selected from image[]
                    switch (i)
                    {
                            #region Pass 1
                            case 0:
                                offX = 0;
                                offY = 0;
                                dX = 8;
                                dY = 8;
                                // Terry originally wrote the cast inside Ceiling to be decimal,
                                // but that's not supported on the 360, so we're converting to double.
                                width = (uint)System.Math.Ceiling((double)header.Width / 8);
                                height = (uint)System.Math.Ceiling((double)header.Height / 8);
                                passes[i] = new byte[(int)System.Math.Ceiling(width * bpp) * height + height];
                                break;
                            #endregion

                            #region Pass 2
                            case 1:
                                offX = 4;
                                offY = 0;
                                dX = 8;
                                dY = 8;
                                width = (uint)System.Math.Ceiling(((double)header.Width - 4) / 8);
                                height = (uint)System.Math.Ceiling((double)header.Height / 8);
                                passes[i] = new byte[(int)System.Math.Ceiling(width * bpp) * height + height];
                                break;
                            #endregion

                            #region Pass 3
                            case 2:
                                offX = 0;
                                offY = 4;
                                dX = 4;
                                dY = 8;
                                width = (uint)System.Math.Ceiling((double)header.Width / 4);
                                height = (uint)System.Math.Ceiling(((double)header.Height - 4) / 8);
                                passes[i] = new byte[(int)System.Math.Ceiling(width * bpp) * height + height];
                                break;
                            #endregion

                            #region Pass 4
                            case 3:
                                offX = 2;
                                offY = 0;
                                dX = 4;
                                dY = 4;
                                width = (uint)System.Math.Ceiling(((double)header.Width - 2) / 4);
                                height = (uint)System.Math.Ceiling((double)header.Height / 4);
                                passes[i] = new byte[(int)System.Math.Ceiling(width * bpp) * height + height];
                                break;
                            #endregion

                            #region Pass 5
                            case 4:
                                offX = 0;
                                offY = 2;
                                dX = 2;
                                dY = 4;
                                width = (uint)System.Math.Ceiling((double)header.Width / 2);
                                height = (uint)System.Math.Ceiling(((double)header.Height - 2) / 4);
                                passes[i] = new byte[(int)System.Math.Ceiling(width * bpp) * height + height];
                                break;
                            #endregion

                            #region Pass 6
                            case 5:
                                offX = 1;
                                offY = 0;
                                dX = 2;
                                dY = 2;
                                width = (uint)System.Math.Ceiling(((double)header.Width - 1) / 2);
                                height = (uint)System.Math.Ceiling((double)header.Height / 2);
                                passes[i] = new byte[(int)System.Math.Ceiling(width * bpp) * height + height];
                                break;
                            #endregion

                            #region Pass 7
                            case 6:
                                offX = 0;
                                offY = 1;
                                dX = 1;
                                dY = 2;
                                width = (uint)header.Width;
                                height = (uint)System.Math.Ceiling(((double)header.Height - 1) / 2);
                                passes[i] = new byte[(int)System.Math.Ceiling(width * bpp) * height + height];
                                break;
                            #endregion
                    }

                    if (i == 0) pointer = 0;
                    else
                    {
                        pointer = 0;
                        for (int j = i - 1; j >= 0; --j)
                        {
                            pointer += (uint)passes[j].Length;
                        }
                    }

                    bps = (uint)(System.Math.Ceiling(bpp * (width)));

                    //Fill the byte[] in passes with the pixel data for this pass
                    // Conversion to (int) needed for 360
                    Array.Copy(image, (int)pointer, passes[i], 0, passes[i].Length);


                    //For each pass, defilter it as if it was its own, independent image
                    pointer = 0;
                    for (int j = 0; j < height; ++j)
                    {
                        //switch on the filter byte of each scanline
                        switch ((int)passes[i][j * (bps + 1)])
                        {
                            //call the appropriate ReverseFilter method, making sure to point to the next byte
                            case 0: break;
                            case 1: ReverseFilterSub(ref passes[i], pointer + 1, bps, bpp, header.BitDepth); break;
                            case 2: ReverseFilterUp(ref passes[i], pointer + 1, bps, bpp, header.BitDepth); break;
                            case 3: ReverseFilterAverage(ref passes[i], pointer + 1, bps, bpp, header.BitDepth); break;
                            case 4: ReverseFilterPaeth(ref passes[i], pointer + 1, bps, bpp, header.BitDepth); break;
                            default:
                                throw new Exception("WRONG!");
                        }

                        //increase the pointer by 1 full scanline(including 1 for the filter byte)
                        pointer += bps + 1;
                    }

                    int x;
                    int y;

                    if (bpp >= 1)
                    {

                        for (float j = 0; j < passes[i].Length; j += bpp)
                        {
                            if (j % (bps + 1) == 0) j += 1;

                            y = (int)(offY + (dY * System.Math.Floor(j / (bps + 1))));
                            x = (int)(offX + (dX * (((j % (bps + 1)) - 1))) / bpp);

                            pointer = (uint)(y * ((bpp * header.Width) + 1) + (x * bpp) + 1);

                            for (int k = 0; k < bpp; ++k)
                            {
                                fullImage[pointer + k] = passes[i][(int)j + k];
                            }


                        }

                    }
                            
                    else
                    {
                        //step = number of pixels contained in one byte
                        int step = (int)(8.0 / header.BitDepth);
                        int bitPointer;
                        byte unshiftedValue;
                        int insertionPointer;
                        int extractionPointer;

                        //iterate through each byte in the current pass
                        for (int bytePointer = 0; bytePointer < passes[i].Length; bytePointer++)
                        {
                            //if this is the first byte in a scanline, skip it(It's the filter byte)
                            if (bytePointer % (bps + 1) == 0) bytePointer += 1;

                            //These are the x and y coordinates of only the FIRST pixel in the byte 
                            y = (int)(offY + (dY * System.Math.Floor((float)bytePointer / (bps + 1))));
                            x = (int)(offX + (dX * (((bytePointer % (bps + 1)) - 1) * step)));

                            for (float j = 0; j < 8; j += header.BitDepth)
                            {
                                int curX = (int)(x + dX * (j / header.BitDepth));
                                if (curX >= header.Width)
                                {
                                    break;
                                } 

                                pointer = (uint)((System.Math.Ceiling(bpp * header.Width) + 1 ) * y);
                                pointer = (uint)(pointer + (curX / step) + 1);

                                bitPointer = (curX % step) * header.BitDepth;
                                insertionPointer = 8 - header.BitDepth - bitPointer;
                                extractionPointer = (int)(8 - header.BitDepth - j);

                                unshiftedValue = ExtractBits(passes[i][bytePointer], extractionPointer, header.BitDepth);

                                    fullImage[pointer] |= (byte)(unshiftedValue << insertionPointer);
                        
                            }
                        }
                    }

                }

                return fullImage;
            }
            #endregion

        }

        private static byte ExtractBits(byte sourceByte, int pointer, byte bitDepth)
        {
            byte toReturn = 0;
            toReturn = (byte)((255 >> (8 - bitDepth)) << pointer);
            toReturn = (byte)(toReturn & sourceByte);
            return (byte)(toReturn >> pointer);
        }

        #endregion

        #region - Chunk Handling/Processing...
        /// <summary>
        /// Reads the bytes of a chunk's ChunkData segment from the Stream and returns it in a
        /// byte[].
        /// </summary>
        /// <param name="file">A Stream pointing to the .png image's data.</param>
        /// <param name="length">The given length of the ChunkData (Should be the value provided by the Chunk).</param>
        /// <returns>A byte[] containing ChunkData from the Stream</returns>
        private static byte[] GetChunkData(ref Stream file, uint length)
        {
            //Read length-number of bytes into a byte[] and return it
            byte[] data = new byte[length];
            for (int i = 0; i < length; ++i)
            {
                data[i] = (byte)file.ReadByte();
            }
            return data;
        }

        /// <summary>
        /// Converts a 4-byte uint into an enumerated ChunkType
        /// </summary>
        /// <param name="type">The 4-byte value read from the .png image to signify a Chunk type</param>
        /// <returns>The enumerated ChunkType matching the uint passed in</returns>
        private static ChunkTypes ConvertToType(uint type)
        {
            switch ((ChunkTypes)type)
            {
                case ChunkTypes.IHDR: return ChunkTypes.IHDR;
                case ChunkTypes.PLTE: return ChunkTypes.PLTE;
                case ChunkTypes.IDAT: return ChunkTypes.IDAT;
                case ChunkTypes.IEND: return ChunkTypes.IEND;
                case ChunkTypes.tRNS: return ChunkTypes.tRNS;
                default: /*System.Diagnostics.Trace.WriteLine(type);*/ return ChunkTypes.UNSUPPORTED;
            }

        }

        /// <summary>
        /// Reads the length, type, data and CRC of the next chunk in the file, and then forms a Chunk
        /// struct using this data.
        /// </summary>
        /// <param name="file">A Stream pointing to the byte data of the .png image.</param>
        /// <param name="chunks">A reference to the List of chunks that this Chunk will be stored in.</param>
        /// <returns>True if the Chunk read isn't the IEND chunk AND the end of the file hasn't been reached.</returns>
        private static bool GetNextChunk(ref Stream file, ref List<Chunk> chunks)
        {
            Chunk newChunk;

            //Read length first
            newChunk.Length = FormUint(ref file);

            //Read the 4-byte type and convert it into an enumeration
            newChunk.Type = ConvertToType(FormUint(ref file));

            //Store the data associated with this Chunk in a byte Stream.
            newChunk.ChunkData = new MemoryStream(GetChunkData(ref file, newChunk.Length));

            //Read the CRC for this Chunk
            newChunk.CRC = FormUint(ref file);

            //Add this chunk to the List
            chunks.Add(newChunk);

            return ((newChunk.Type != ChunkTypes.IEND) && (file.Position < file.Length));
        }

        /// <summary>
        /// Processes the IHDR chunk by reading and storing all of its information in a HeaderInfo struct.
        /// </summary>
        /// <param name="chunks">A List containing all of the Chunks for this .png image</param>
        /// <returns>A HeaderInfo struct containing all of the information of this images IHDR chunk</returns>
        private static HeaderInfo ReadHeader(ref List<Chunk> chunks)
        {
            //IHDR is required to be the first chunk in a .png image. Check the
            //type of the first Chunk in List chunks and make sure that it's
            //IHDR.
            Chunk ihdr = chunks[0];

            if (ihdr.Type != ChunkTypes.IHDR)
                throw new ArgumentException(String.Format(
                    "List of chunks contains no chunk for header information, or it is not first in the list."));
            //If so, remove it from the list
            else
                chunks.RemoveAt(0);

            //Read each field of data from IHDR's ChunkData Stream
            HeaderInfo header;
            Stream stream = ihdr.ChunkData;
            header.Width = FormUint(ref stream);                    //System.Diagnostics.Trace.WriteLine(String.Format("Width:{0}",header.Width));
            header.Height = FormUint(ref stream);                  // System.Diagnostics.Trace.WriteLine(String.Format("Height:{0}",header.Height));
            header.BitDepth = (byte)stream.ReadByte();              //System.Diagnostics.Trace.WriteLine(String.Format("BitDepth:{0}",header.BitDepth));
            header.ColorType = (byte)stream.ReadByte();             //System.Diagnostics.Trace.WriteLine(String.Format("ColorType:{0}",header.ColorType));
            header.CompressionMethod = (byte)stream.ReadByte();    // System.Diagnostics.Trace.WriteLine(String.Format("Compression:{0}",header.CompressionMethod));
            header.FilterMethod = (byte)stream.ReadByte();         // System.Diagnostics.Trace.WriteLine(String.Format("Filter:{0}",header.FilterMethod));
            header.InterlaceMethod = (byte)stream.ReadByte();      // System.Diagnostics.Trace.WriteLine(String.Format("Interlace:{0}",header.InterlaceMethod));

          /*  if (header.InterlaceMethod != 0)
                throw new ArgumentException(string.Format(
                    "FlatRedBall does not currently support interlaced .png images."));
            */
            //Close the Stream
            stream.Close();

            return header;
        }

        /// <summary>
        /// Processes the PLTE chunk by reading in all of the indexed color values and storing them in
        /// a PaletteInfo struct.
        /// </summary>
        /// <param name="plte">The Chunk containing the PLTE chunk from the file</param>
        /// <param name="palette">An unused PaletteInfo struct to be filled by data from plte</param>
        private static void ReadPalette(Chunk plte, ref PaletteInfo palette)
        {
            //The length of PLTE's data segment should be 3 * the number of entries
            uint numEntries = (uint)plte.ChunkData.Length / 3;

            //Checking for monkey-business???
            if ((numEntries * 3) == plte.ChunkData.Length)
            {
                //Read in the values of the color entries and store them.
                for (int i = 0; i < numEntries; ++i)
                {
                    RGB value = new RGB();
                    value.R = (byte)plte.ChunkData.ReadByte(); //System.Diagnostics.Trace.WriteLine(value.R);
                    value.G = (byte)plte.ChunkData.ReadByte();// System.Diagnostics.Trace.WriteLine(value.G);
                    value.B = (byte)plte.ChunkData.ReadByte();// System.Diagnostics.Trace.WriteLine(value.B);
                    palette.Entries[i] = value;
                }
            }
        }

        static byte[] sReadImageDataByteBuffer = new byte[4096];

        /// <summary>
        /// Takes a List of all of the Chunks in the .png image being loaded (after IHDR is loaded and removed) 
        /// and processes each Chunk appropriately before returning the compressed image. NOTE: Currently only 
        /// processes the required Chunks PLTE, IDAT and IEND. Ancillary chunks are currently unsupported.
        /// </summary>
        /// <param name="chunks">The List of chunks loaded from the .png image file</param>
        /// <param name="header">The HeaderInfo loaded from the IHDR chunk</param>
        /// <param name="palette">An unused PaletteInfo struct to load the PLTE chunk data into</param>
        /// <returns>A byte[] containing the constructed and decompressed image</returns>
        private static byte[] ProcessChunks(List<Chunk> chunks, HeaderInfo header, ref PaletteInfo palette)
        {
            int numberOfBytesRead = 0;

            foreach (Chunk currentChunk in chunks)
            {

                switch (currentChunk.Type)
                {
                    //Only one IHDR can exist, and it should have already been removed
                    case ChunkTypes.IHDR:
                        {
                            throw new Exception(String.Format("List<Chunk> chunks still contains IHDR chunk info."));
                            //break;
                        }

                    case ChunkTypes.PLTE:
                        {   //Check if a palette can even be used for this image's ColorType
                            if ((header.ColorType == 0) || (header.ColorType == 4))
                                throw new Exception(String.Format(
                                    "The given ColorType, {0}, does not support the use of a Palette.", header.ColorType));
                            //if so, prepare the palette to hold the maximum number of colors for the given bitDepth
                            palette.Entries = new RGB[(1 << (header.BitDepth + 1)) - 1];
                            //Read the PLTE chunk into the given PaletteInfo
                            ReadPalette(currentChunk, ref palette);
                            break;
                        }

                    case ChunkTypes.IDAT:
                        {
                            //Read the IDAT chunk's data into the List for imageData
                            ReadImageData(currentChunk, ref sReadImageDataByteBuffer, ref numberOfBytesRead);
                            break;
                        }

                    case ChunkTypes.IEND:
                        {
                            //This should be the last chunk to be loaded, so it's safe now to decompress the image
                            //and do any extra processing caused by ancillary chunks.

                            //Check if there are any chunks after IEND
                            if (chunks.IndexOf(currentChunk) != chunks.Count - 1)
                                throw new Exception(String.Format(
                                    "IEND chunk does not appear to be the last chunk in the image."));

                            //Load the compressed image into an InflaterInputStream to inflate(decompress) the image
                            MemoryStream streamForInflater = new MemoryStream(sReadImageDataByteBuffer, 0, numberOfBytesRead);

                            InflaterInputStream decompressionStream = new InflaterInputStream(streamForInflater);

                            //Read the decompressed image through the stream and into a byte[]
                            ReadStream(ref decompressionStream);

                            break;
                        }
                    case ChunkTypes.tRNS:
                        {
                            mMaxTransIndex = (int)currentChunk.Length - 1;
                            for (int i = 0; i < currentChunk.Length; ++i)
                            {
                                mTransparentEntries[i] = (byte)currentChunk.ChunkData.ReadByte();
                            }
                            break;
                        }


                    //Represents an ancillary chunk that isn't yet implemented or an unknown chunk type
                    case ChunkTypes.UNSUPPORTED:
                        {
                            break;
                        }
                }
            }


            //Return the image
            return sByteBuffer;
        }
        #endregion

        #region - Private Method(s)...
        /// <summary>
        /// Checks the first 8 bytes of the file in the given Stream against the standard signature
        /// for .png images. 
        /// </summary>
        /// <param name="file">The byte Stream pointing to data from the .png file</param>
        /// <returns>True if the Stream file contains a valid .png image</returns>
        private static bool CheckSignature(ref Stream file)
        {
            //8-byte .png signature
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

            for (int i = 0; i < 8; ++i)
            {
                if (signature[i] != file.ReadByte()) return false;
            }

            return true;
        }




        /// <summary>
        /// Reads data from the given IDAT chunk and appends it to the List imageData.
        /// </summary>
        /// <param name="idat">A Chunk containing a compressed section of the image.</param>
        /// <param name="imageData">A List of bytes to hold the entire compressed image.</param>
        private static void ReadImageData(Chunk idat, ref byte[] imageData, ref int numberOfBytesRead)
        {
            while (idat.Length > imageData.Length - numberOfBytesRead)
            {
                byte[] newArray = new byte[idat.Length + imageData.Length];
                try
                {
                    Array.Copy(imageData, newArray, imageData.Length);
                }
                catch (Exception )
                {

                }
                imageData = newArray;
            }

            for (int i = 0; i < idat.Length; ++i)
            {
                imageData[numberOfBytesRead] = (byte)idat.ChunkData.ReadByte();
                numberOfBytesRead++;
            }

            //TODO: May consider using CRC here

            //Check that the proper number of bytes were read.
            if (idat.ChunkData.Position != idat.ChunkData.Length)
            {
                throw new Exception(
                    String.Format(
                    "Length of data in IDAT doesn't match the length provided by the chunk. " +
                    "Bytes read: {0} " +
                    "Bytes expected: {1}.",
                    (uint)idat.ChunkData.Position, idat.Length));

            }

        }

        /// <summary>
        /// Reads the decompressed(inflated) image data from an InflaterInputStream that was fed the compressed
        /// image.
        /// </summary>
        /// <param name="array">The byte[] to store the decompressed bytes of the image in.</param>
        /// <param name="stream">The InflaterInputStream that contains the deflated(compressed) image.</param>
        private static void ReadStream(ref InflaterInputStream stream)
        {
            int totalRead = 0;
            int read;

            //Read as many bytes as possible into array
            while ((read = stream.Read(sByteBuffer, totalRead, sByteBuffer.Length - totalRead)) > 0)
            {
                //update the total number of bytes read
                totalRead += read;

                //if array is full
                if (totalRead == sByteBuffer.Length)
                {

                    //Copy into a larger array and repeat
                    byte[] newArray = new byte[sByteBuffer.Length * 2];
                    Array.Copy(sByteBuffer, newArray, sByteBuffer.Length);
                    sByteBuffer = newArray;
                }
            }
        }


        /// <summary>
        /// Does the remaining work to convert the byte[] containing the image into a Color[] containing colors
        /// for each pixel. This operation is different for most individual combinations of ColorType and bitDepth 
        /// and currently only ColorType 2 is supported.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="header"></param>
        /// <param name="palette"></param>
        /// <returns></returns>
        private static Color[] BuildPixels(byte[] image, ref HeaderInfo header, ref PaletteInfo palette)
        {
            //System.Diagnostics.Trace.WriteLine(String.Format("ColorType: {0}\tBitDepth: {1}", header.ColorType, header.BitDepth));
           // System.Diagnostics.Trace.WriteLine(String.Format("Width: {0}\tHeight: {1}", header.Width, header.Height));

            int totalPixels = (int)(header.Width * header.Height);
            int currentIndex = 0;

            if (totalPixels > mStaticPixels.Length)
            {
                mStaticPixels = new Color[totalPixels];
            }


            //MemoryStream imageStream = new MemoryStream(image);
            int indexInImage = 0;

            int filterByte;

            switch ((int)header.ColorType)
            {
                //Each pixel is an R,G,B triple
                case 2:
                    {
                        //This section of code containing conversion of 16-bit samples is currently untested.
                        if (header.BitDepth > 8)
                        {
                            int bps = (int)(header.Width * header.BitDepth * 3 + 1);
                            //int R = 0;
                            //int G = 0;
                            //int B = 0;
                            while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                            {
                                for (int i = 0; i < header.Width; ++i)
                                {
                                    mStaticPixels[currentIndex].R = image[indexInImage++]; indexInImage++;
                                    mStaticPixels[currentIndex].G = image[indexInImage++]; indexInImage++;
                                    mStaticPixels[currentIndex].B = image[indexInImage++]; indexInImage++;
                                    mStaticPixels[currentIndex].A = byte.MaxValue;
                                    currentIndex++;
                                }
                            }

                        }
                        else
                        {
                            while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                            {
                                for (int i = 0; i < header.Width; ++i)
                                {
                                    mStaticPixels[currentIndex].R = image[indexInImage++];
                                    mStaticPixels[currentIndex].G = image[indexInImage++];
                                    mStaticPixels[currentIndex].B = image[indexInImage++];
                                    mStaticPixels[currentIndex].A = byte.MaxValue;
                                    currentIndex++;
                                }
                            }
                        }
                        break;
                    }

                //Each pixel is a grayscale sample
                case 0:
                    {

                        if (header.BitDepth < 8)
                        {
                            byte sample;
                            int value;
                            float grayValue;
                            int numBits = 8 / header.BitDepth;
                            int mask = (255 >> (8 - header.BitDepth));
                            while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                            {
                                for (int i = 0; i < header.Width / numBits; ++i)
                                {
                                    value = image[indexInImage++];
                                    for (int j = 8 - header.BitDepth; j >= 0; j -= header.BitDepth)
                                    {
                                        grayValue = (float)((value & (mask << j)) >> j);
                                        sample = (byte)((grayValue / ((1 << header.BitDepth) - 1)) * 255);
                                        mStaticPixels[currentIndex].R = sample;
                                        mStaticPixels[currentIndex].G = sample;
                                        mStaticPixels[currentIndex].B = sample;
                                        mStaticPixels[currentIndex].A = byte.MaxValue;
                                        currentIndex++;
                                    }
                                }
                            }

                        }
                        else if (header.BitDepth == 8)
                        {
                            byte sample;
                            float value;

                            //Get the filter byte out of the way
                            while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                            {
                                //Read the scanline
                                for (int i = 0; i < header.Width; ++i)
                                {
                                    value = (float)(image[indexInImage++]);
                                    sample = (byte)((value / ((1 << header.BitDepth) - 1)) * 255);
                                    mStaticPixels[currentIndex].R = sample;
                                    mStaticPixels[currentIndex].G = sample;
                                    mStaticPixels[currentIndex].B = sample;
                                    mStaticPixels[currentIndex].A = byte.MaxValue;
                                    currentIndex++;
                                }
                            }
                        }

                        else
                        {
                            byte sample;
                            while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                            {
                                for (int i = 0; i < header.Width; ++i)
                                {
                                    sample = image[indexInImage++]; indexInImage++;
                                    mStaticPixels[currentIndex].R = sample;
                                    mStaticPixels[currentIndex].G = sample;
                                    mStaticPixels[currentIndex].B = sample;
                                    mStaticPixels[currentIndex].A = byte.MaxValue;
                                    currentIndex++;
                                }
                            }
                        }

                        break;
                    }

                //Each pixel is a palette index
                case 3:
                    {
                        RGB entry;
                        int value;
                        int numBits = 8 / header.BitDepth;
                        int mask = (255 >> (8 - header.BitDepth));
                        int index;

                        long headerWidthDividedByNumBits = header.Width / numBits;

                        while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                        {
                            for (int i = 0; i < headerWidthDividedByNumBits; ++i)
                            {
                                value = image[indexInImage++];
                                for (int j = 8 - header.BitDepth; j >= 0; j -= header.BitDepth)
                                {
                                    index = (value & (mask << j)) >> j;
                                    entry = palette.Entries[index];
                                    mStaticPixels[currentIndex].R = entry.R;
                                    mStaticPixels[currentIndex].G = entry.G;
                                    mStaticPixels[currentIndex].B = entry.B;

                                    //if (mMaxTransIndex >= index)
                                    {
                                        mStaticPixels[currentIndex].A = mTransparentEntries[index];
                                    }
                                    currentIndex++;
                                }
                            }
                        }
                        break;
                    }

                //Each pixel is a grayscale sample, followed by an alpha sample
                case 4:
                    {
                        switch ((int)header.BitDepth)
                        {
                            case 8:
                                {
                                    //Open stream to image array
                                    byte sample;

                                    //Get the filter byte out of the way
                                    while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                                    {
                                        //Read the scanline
                                        for (int i = 0; i < header.Width; ++i)
                                        {
                                            sample = image[indexInImage++];
                                            mStaticPixels[currentIndex].R = sample;
                                            mStaticPixels[currentIndex].G = sample;
                                            mStaticPixels[currentIndex].B = sample;
                                            mStaticPixels[currentIndex].A = image[indexInImage++];
                                            currentIndex++;
                                        }
                                    }

                                    break;
                                }

                            case 16:
                                {
                                    byte sample;
                                    while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                                    {
                                        for (int i = 0; i < header.Width; ++i)
                                        {
                                            sample = image[indexInImage++]; indexInImage++;
                                            mStaticPixels[currentIndex].R = sample;
                                            mStaticPixels[currentIndex].G = sample;
                                            mStaticPixels[currentIndex].B = sample;
                                            mStaticPixels[currentIndex].A = image[indexInImage++]; 
                                            indexInImage++;
                                            currentIndex++;
                                        }
                                    }

                                    break;
                                }
                        }
                        break;
                    }

                //Each pixel is an R,G,B triple, followed by an alpha sample
                case 6:
                    {
                        switch ((int)header.BitDepth)
                        {
                            case 8:
                                {
                                    while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                                    {
                                        for (int i = 0; i < header.Width; ++i)
                                        {
                                            mStaticPixels[currentIndex].R = image[indexInImage++];
                                            mStaticPixels[currentIndex].G = image[indexInImage++];
                                            mStaticPixels[currentIndex].B = image[indexInImage++];
                                            mStaticPixels[currentIndex].A = image[indexInImage++];

                                            currentIndex++;
                                        }
                                    }
                                    break;
                                }

                            case 16:
                                {
                                    int bps = (int)(header.Width * header.BitDepth * 3 + 1);
                                    //int R = 0;
                                    //int G = 0;
                                    //int B = 0;
                                    //int A = 0;
                                    while ((filterByte = image[indexInImage++]) > -1 && currentIndex < totalPixels)
                                    {
                                        for (int i = 0; i < header.Width; ++i)
                                        {
                                            mStaticPixels[currentIndex].R = image[indexInImage++]; indexInImage++;
                                            mStaticPixels[currentIndex].G = image[indexInImage++]; indexInImage++;
                                            mStaticPixels[currentIndex].B = image[indexInImage++]; indexInImage++;
                                            mStaticPixels[currentIndex].A = image[indexInImage++]; indexInImage++;

                                            currentIndex++;
                                        }
                                    }

                                    break;
                                }
                        }
                        break;
                    }

            }
            //imageStream.Close();
            return mStaticPixels;
        }

        private static Color[] OldMethod(byte[] image, ref HeaderInfo header, ref PaletteInfo palette)
        {
            int totalPixels = (int)(header.Width * header.Height);
            int currentIndex = 0;

            if (totalPixels > mStaticPixels.Length)
            {
                mStaticPixels = new Color[totalPixels];
            }

            MemoryStream imageStream = new MemoryStream(image);
            int filterByte;

            switch ((int)header.ColorType)
            {
                //Each pixel is an R,G,B triple
                case 2:
                    {
                        //This section of code containing conversion of 16-bit samples is currently untested.
                        if (header.BitDepth > 8)
                        {
                            int bps = (int)(header.Width * header.BitDepth * 3 + 1);
                            int R = 0;
                            int G = 0;
                            int B = 0;
                            while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                            {
                                for (int i = 0; i < header.Width; ++i)
                                {
                                    R = imageStream.ReadByte(); imageStream.ReadByte();
                                    G = imageStream.ReadByte(); imageStream.ReadByte();
                                    B = imageStream.ReadByte(); imageStream.ReadByte();
                                    mStaticPixels[currentIndex] = (new Color((byte)R, (byte)G, (byte)B));
                                    currentIndex++;
                                }
                            }

                        }
                        else
                        {
                            while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                            {
                                for (int i = 0; i < header.Width; ++i)
                                {
                                    mStaticPixels[currentIndex] = (new Color(
                                        (byte)imageStream.ReadByte(),
                                        (byte)imageStream.ReadByte(),
                                        (byte)imageStream.ReadByte()));
                                    currentIndex++;
                                }
                            }
                        }
                        break;
                    }

                //Each pixel is a grayscale sample
                case 0:
                    {

                        if (header.BitDepth < 8)
                        {
                            byte sample;
                            int value;
                            float grayValue;
                            int numBits = 8 / header.BitDepth;
                            int mask = (255 >> (8 - header.BitDepth));
                            while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                            {
                                for (int i = 0; i < header.Width / numBits; ++i)
                                {
                                    value = imageStream.ReadByte();
                                    for (int j = 8 - header.BitDepth; j >= 0; j -= header.BitDepth)
                                    {
                                        grayValue = (float)((value & (mask << j)) >> j);
                                        sample = (byte)((grayValue / ((1 << header.BitDepth) - 1)) * 255);
                                        mStaticPixels[currentIndex] = (new Color(sample, sample, sample));
                                        currentIndex++;
                                    }
                                }
                            }

                        }
                        else if (header.BitDepth == 8)
                        {
                            byte sample;
                            float value;

                            //Get the filter byte out of the way
                            while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                            {
                                //Read the scanline
                                for (int i = 0; i < header.Width; ++i)
                                {
                                    value = (float)(imageStream.ReadByte());
                                    sample = (byte)((value / ((1 << header.BitDepth) - 1)) * 255);
                                    mStaticPixels[currentIndex] = (new Color(sample, sample, sample));
                                    currentIndex++;
                                }
                            }
                        }

                        else
                        {
                            byte sample;
                            while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                            {
                                for (int i = 0; i < header.Width; ++i)
                                {
                                    sample = (byte)imageStream.ReadByte(); imageStream.ReadByte();
                                    mStaticPixels[currentIndex] = (new Color(sample, sample, sample));
                                    currentIndex++;
                                }
                            }
                        }

                        break;
                    }

                //Each pixel is a palette index
                case 3:
                    {
                        RGB entry;
                        int value;
                        int numBits = 8 / header.BitDepth;
                        int mask = (255 >> (8 - header.BitDepth));
                        int index;
                        while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                        {
                            for (int i = 0; i < header.Width / numBits; ++i)
                            {
                                value = imageStream.ReadByte();
                                for (int j = 8 - header.BitDepth; j >= 0; j -= header.BitDepth)
                                {
                                    index = (value & (mask << j)) >> j;
                                    entry = palette.Entries[index];
                                    mStaticPixels[currentIndex] = (new Color((byte)entry.R, (byte)entry.G, (byte)entry.B));

                                    if (mMaxTransIndex >= index)
                                    {
                                        mStaticPixels[currentIndex].A = mTransparentEntries[index];
                                    }
                                    currentIndex++;
                                }
                            }
                        }
                        break;
                    }

                //Each pixel is a grayscale sample, followed by an alpha sample
                case 4:
                    {
                        switch ((int)header.BitDepth)
                        {
                            case 8:
                                {
                                    //Open stream to image array
                                    byte sample;

                                    //Get the filter byte out of the way
                                    while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                                    {
                                        //Read the scanline
                                        for (int i = 0; i < header.Width; ++i)
                                        {
                                            sample = (byte)(imageStream.ReadByte());
                                            mStaticPixels[currentIndex] = (new Color(sample, sample, sample, (byte)imageStream.ReadByte()));
                                            currentIndex++;
                                        }
                                    }

                                    break;
                                }

                            case 16:
                                {
                                    byte sample;
                                    while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                                    {
                                        for (int i = 0; i < header.Width; ++i)
                                        {
                                            sample = (byte)imageStream.ReadByte(); imageStream.ReadByte();
                                            mStaticPixels[currentIndex] = (new Color(sample, sample, sample, (byte)imageStream.ReadByte())); imageStream.ReadByte();
                                            currentIndex++;
                                        }
                                    }

                                    break;
                                }
                        }
                        break;
                    }

                //Each pixel is an R,G,B triple, followed by an alpha sample
                case 6:
                    {
                        switch ((int)header.BitDepth)
                        {
                            case 8:
                                {
                                    while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                                    {
                                        for (int i = 0; i < header.Width; ++i)
                                        {
                                            mStaticPixels[currentIndex] = (new Color(
                                                (byte)imageStream.ReadByte(),
                                                (byte)imageStream.ReadByte(),
                                                (byte)imageStream.ReadByte(),
                                                (byte)imageStream.ReadByte()));

                                            currentIndex++;
                                        }
                                    }
                                    break;
                                }

                            case 16:
                                {
                                    int bps = (int)(header.Width * header.BitDepth * 3 + 1);
                                    int R = 0;
                                    int G = 0;
                                    int B = 0;
                                    int A = 0;
                                    while ((filterByte = imageStream.ReadByte()) > -1 && currentIndex < totalPixels)
                                    {
                                        for (int i = 0; i < header.Width; ++i)
                                        {
                                            R = imageStream.ReadByte(); imageStream.ReadByte();
                                            G = imageStream.ReadByte(); imageStream.ReadByte();
                                            B = imageStream.ReadByte(); imageStream.ReadByte();
                                            A = imageStream.ReadByte(); imageStream.ReadByte();
                                            mStaticPixels[currentIndex] = (new Color((byte)R, (byte)G, (byte)B, (byte)A));
                                            currentIndex++;
                                        }
                                    }

                                    break;
                                }
                        }
                        break;
                    }

            }
            imageStream.Close();
            return mStaticPixels;
        }
        #endregion

        #region - Public Method(s)...
        /// <summary>
        /// Loads a .png image from the fileName given and returns an array of pixel colors.
        /// </summary>
        /// <param name="fileName">The name of the .png image to be loaded.</param>
        /// <returns>
        /// Returns a Microsoft.Xna.Framework.Graphics.Color[] containing one item(Color) for each pixel
        /// in the image.</returns>
        public static ImageData GetPixelData(string fileName)
        {
            for (int i = 0; i < ByteBufferSize; i++)
            {
                mTransparentEntries[i] = byte.MaxValue;
            }

            int bytesRead = LoadFile(fileName);

            


            mMaxTransIndex = -1;


            //Load the file into "byte stream"
            Stream sourceStream = new MemoryStream(sByteBuffer, 0, bytesRead);


            //Check the signature to verify this is an actual .png image
            if (!CheckSignature(ref sourceStream))
                throw new ArgumentException(
                    String.Format("Argument Stream {0} does not contain a valid PNG file.", sourceStream));

            //Since all data in .png files is organized into categorized chunks, create a
            //List to store them in so that they can be read and processed later.
            List<Chunk> chunks = new List<Chunk>();

            //Load each Chunk of data from the stream into the List of Chunks. Then
            //close the stream.
            while (GetNextChunk(ref sourceStream, ref chunks)) { }
            sourceStream.Close();


            //Read and store the information from the IHDR chunk, which contains
            //general info for this image. Note: IHDR chunk is removed from List<Chunk> chunks.
            HeaderInfo header = ReadHeader(ref chunks);


            //Create an empty palette in case we need it
            PaletteInfo palette = new PaletteInfo();

            //Process the Chunks of data and obtain the decompressed bytes of the image
            byte[] filteredImage = ProcessChunks(chunks, header, ref palette);


            //Reverse the filtering that was done on the image before compression
            byte[] defilteredImage = ReverseFiltering(filteredImage, header);



            //Translate the un-filtered image bytes into Colors and store in the array to be returned.
            Color[] pixelData = BuildPixels(defilteredImage, ref header, ref palette);
            //System.Diagnostics.Trace.WriteLine(pixelData.Length);

            ImageData imageData = new ImageData((int)header.Width, (int)header.Height, pixelData);


            return imageData;
        }
        #endregion

    }

}