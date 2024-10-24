using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using FlatRedBall.IO.Gif;
using FlatRedBall.Graphics.Texture;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;


namespace FlatRedBall.IO
{
    /// <summary>
    /// Contains an indexed array of Colors to be used by images with ColorType 3 and possibly ColorTypes
    /// 2 and 6.
    /// </summary>
    internal struct PaletteInfo
    {
        public RGB[] Entries;
    }

    /// <summary>
    /// Simple struct used to hold sample values.
    /// </summary>
    internal struct RGB
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
    }


    /// <summary>
    /// Class responsible for loading GIF files.
    /// </summary>
    /// <remarks>
    /// For information, see:
    /// http://www.fileformat.info/format/gif/
    /// </remarks>

    public static class GifLoader
    {
        #region Structs

        private struct GifInfo
        {
            public string Header;
            public int Width;
            public int Height;
            public bool HasGlobalColorTable;
            public int NumberOfColorEntries;
            public PaletteInfo PaletteInfo;
            public bool HasTransparency;
            public int TransparentIndex;
            public int LzwMin; // the starting number of bits to read
            public List<Int16> DelayTimes;
            public bool IsInterlaced;

            public Int16 CurrentBlockLeft;
            public Int16 CurrentBlockTop;
            public Int16 CurrentBlockWidth;
            public Int16 CurrentBlockHeight;

        }
        #endregion

        #region Fields

        static int ClrConstant;
        static int EndConstant;


        static List<List<int>> mDictionary;

        static List<byte> mUncompressedColorIndexBuffer;

        static GifInfo mGifInfo;

        #endregion

        #region Properties

        public static int CurrentEntrySize
        {
            get
            {
                int numberOfBits = 0;

                while ((1 << numberOfBits) <= mDictionary.Count)
                {
                    numberOfBits++;
                }

                return numberOfBits;
            }
        }

        #endregion

        #region Methods

        #region Public Methods

        public static ImageDataList GetImageDataList(string gifFileName)
        {
            ImageDataList imageDatas = new ImageDataList();

            using (FileStream stream = System.IO.File.OpenRead(gifFileName))
            {
                mGifInfo = new GifInfo();
                mGifInfo.DelayTimes = new List<short>();

                BinaryReader reader = new BinaryReader(stream);

                #region header
                // Example:  GIF89a
                byte[] buffer = reader.ReadBytes(6);
                // set the header here
                char[] charArray = new char[buffer.Length];
                for (int i = 0; i < buffer.Length; i++)
                {
                    charArray[i] = (char)buffer[i];
                }

                mGifInfo.Header = new string(charArray);
                #endregion

                #region Width/Height

                mGifInfo.Width = reader.ReadInt16();
                mGifInfo.Height = reader.ReadInt16();

                #endregion

                #region Packed information

                // The packed byte has the following info:
                // Bits 0-2     Size of the Global Color Table
                // Bit 3        Color Table Sort Flag
                // Bits 4-6     Color Resolution
                // Bit 7        Global Color Table Flag

                Byte packedByte = reader.ReadByte();

                int sizeOfGlobalColorTable = (7 & packedByte);

                mGifInfo.NumberOfColorEntries = 1 * (1 << (sizeOfGlobalColorTable + 1));

                mGifInfo.HasGlobalColorTable = (128 & packedByte) != 0;

                #endregion

                #region background color
                reader.ReadByte();
                #endregion

                #region default aspect ratio

                reader.ReadByte();

                #endregion

                TryReadGlobalColorTable(reader);


                byte separator = reader.ReadByte();

                while (separator != 0x3b) // Extension
                {
                    switch(separator)
                    {
                        #region Extensions

                        case 0x21:

                            Byte label = reader.ReadByte();

                            switch (label)
                            {
                                case 0xf9:
                                    ReadGraphicsControlExtension(reader);
                                    break;
                                case 0xff:
                                    ReadApplicationExtensionBlock(reader);
                                    break;
                                case 0xfe:
                                    ReadCommonExtensionBlock(reader);
                                    break;

                            }
                            break;
                        #endregion

                        #region Image Data
                        case 0x2c:

                            ReadImageDescriptor(reader);


                            Color[] color = new Color[mGifInfo.Width * mGifInfo.Height];

                            int transparentIndex = mGifInfo.TransparentIndex;

                            #region Interlaced
                            if (mGifInfo.IsInterlaced)
                            {
                                int i = 0;
                                for (int pass = 0; pass < 4; ++pass)
                                {
                                    int row = 0;
                                    int increment = 0;

                                    switch (pass)
                                    {
                                        case 0: row = 0; increment = 8; break;
                                        case 1: row = 4; increment = 8; break;
                                        case 2: row = 2; increment = 4; break;
                                        case 3: row = 1; increment = 2; break;
                                    }
                                    for (; row < mGifInfo.Height; row += increment)
                                    {

                                        for (int col = 0; col < mGifInfo.Width; ++col)
                                        {
                                            int position = (row * mGifInfo.Width) + col;

                                            byte index = mUncompressedColorIndexBuffer[i++];

                                            byte alpha = 255;

                                            if (mGifInfo.HasTransparency && index == transparentIndex)
                                                alpha = 0;

                                        color[position] = new Color(
                                            (byte)mGifInfo.PaletteInfo.Entries[index].R,
                                            (byte)mGifInfo.PaletteInfo.Entries[index].G,
                                            (byte)mGifInfo.PaletteInfo.Entries[index].B,
                                            (byte)alpha);
                                        }

                                    }

                                }
                            }
                            #endregion
                            else
                            {
                                #region NonInterlaced

                                for (int i = 0; i < mGifInfo.PaletteInfo.Entries.Length; i++)
                                {
                                    mGifInfo.PaletteInfo.Entries[transparentIndex].A = 255;
                                }                                
                                if (mGifInfo.HasTransparency)
                                {
                                    mGifInfo.PaletteInfo.Entries[transparentIndex].A = 0;
                                }

                                int x = 0;
                                int y = 0;

                                int colorIndex;

                                for (int i = 0; i < mUncompressedColorIndexBuffer.Count; i++)
                                {                                    
                                    byte index = mUncompressedColorIndexBuffer[i];

                                    // Let's see if we can avoid an if statement
                                    x = mGifInfo.CurrentBlockLeft + i % mGifInfo.CurrentBlockWidth;
                                    y = mGifInfo.CurrentBlockTop + i / mGifInfo.CurrentBlockWidth;

                                    colorIndex = x + y * mGifInfo.Width;

                                    color[colorIndex] = new Color(
                                        (byte)mGifInfo.PaletteInfo.Entries[index].R,
                                        (byte)mGifInfo.PaletteInfo.Entries[index].G,
                                        (byte)mGifInfo.PaletteInfo.Entries[index].B,
                                        (byte)mGifInfo.PaletteInfo.Entries[index].A);
                                }
                                #endregion
                            }
                            ImageData imageData = new ImageData(
                                mGifInfo.Width, mGifInfo.Height, color);

                            imageDatas.Add(imageData);

                            mUncompressedColorIndexBuffer.Clear();

                            break;
                        #endregion

                        #region End of file
                        case 0x3b:

                            // end of file
                            break;

                        #endregion
                    }

                    separator = reader.ReadByte();
                }
            }

            // Fill the imageDatas with the frame times
            foreach (short s in mGifInfo.DelayTimes)
            {
                imageDatas.FrameTimes.Add(s / 100.0);
            }

            return imageDatas;
        }

        #endregion

        #region Private Methods

        private static void FillDictionary()
        {
            mDictionary = new List<List<int>>();

            for (int i = 0; i < mGifInfo.NumberOfColorEntries; i++)
            {
                mDictionary.Add(new List<int> { i });
            }

            ClrConstant = (1 << mGifInfo.LzwMin);
            EndConstant = ClrConstant + 1;

            while (mDictionary.Count < ClrConstant)
            {
                mDictionary.Add(new List<int>());
            }

            mDictionary.Add(new List<int> { ClrConstant });
            mDictionary.Add(new List<int> { EndConstant });
        }

        private static void FillUncompressedColorIndexBuffer(byte[] data, ref int currentEntrySize, ref bool firstRead)
        {
            BitReader reader = new BitReader(data);

            List<int> nextDictionaryEntry = new List<int>();
            while ((reader.BitPosition + currentEntrySize) <= reader.BitLength)
            {
                int result = reader.Read(currentEntrySize);
                bool addToDictionary =
                    !firstRead &&// don't add on the first read
                    (mDictionary.Count < (1 << 12)); // dictionary capped at 12 bits 

                List<int> pattern;
                if (result < mDictionary.Count)
                {
                    pattern = mDictionary[result];
                }
                else if (result == mDictionary.Count)
                {
                    // special case for LZW
                    pattern = new List<int>(nextDictionaryEntry);
//                    pattern.Add(nextDictionaryEntry[nextDictionaryEntry.Count - 1]);
                    pattern.Add(nextDictionaryEntry[0]);

                }
                else
                {
                    int index = mUncompressedColorIndexBuffer.Count - 1;

                    string error = "Error when reading pixel number " + (mUncompressedColorIndexBuffer.Count - 1);

                    int row = (mUncompressedColorIndexBuffer.Count) / mGifInfo.Width + 1;
                    int column = (mUncompressedColorIndexBuffer.Count) % mGifInfo.Width;


                    error += "\n On the image that's (" + column + ", " + row + ").";

                    throw new Exception(error);
                }


                firstRead = false;

                // Check for constant values
                if (result == ClrConstant)
                {
                    // Clear dictionary
                    addToDictionary = false;
                    firstRead = true;
                    nextDictionaryEntry = new List<int>();
                    currentEntrySize = mGifInfo.LzwMin + 1;
                    FillDictionary();
                }
                else if (result == EndConstant)
                {
                    // End of image
                    break;
                }
                else
                {
                    // It's just image data
                    for (int i = 0; i < pattern.Count; i++)
                    {
                        mUncompressedColorIndexBuffer.Add((byte)pattern[i]);
                    }
#if DEBUG
                    if (mUncompressedColorIndexBuffer.Count > mGifInfo.Width * mGifInfo.Height)
                    {
                        throw new InvalidOperationException("The uncompressed buffer is too big!");
                    }
#endif
                }

                
                if (addToDictionary)
                {
                    // add to dictionary
  
                    List<int> newPattern = new List<int>(nextDictionaryEntry);
//                    newPattern.Add(pattern[pattern.Count - 1]);
                    newPattern.Add(pattern[0]);

                    mDictionary.Add(newPattern);

                    currentEntrySize = CurrentEntrySize;

                    if (currentEntrySize == 13)
                    {
                        currentEntrySize = 12;
                    }
                }
                if (result != ClrConstant)
                {
                    nextDictionaryEntry = pattern;
                } 
            }



        }
        
        private static void ReadApplicationExtensionBlock(BinaryReader reader)
        {
            byte blockSize = reader.ReadByte();



            char[] identifier = reader.ReadChars(8);
            byte[] authentCode = reader.ReadBytes(3);

            byte howManyBytes = reader.ReadByte();

            while (howManyBytes != 0)
            {
                reader.ReadBytes(howManyBytes);

                howManyBytes = reader.ReadByte();
            }
        }

        private static void ReadCommonExtensionBlock(BinaryReader reader)
        {
            byte howManyBytes = reader.ReadByte();

            while (howManyBytes != 0)
            {
                reader.ReadBytes(howManyBytes);

                howManyBytes = reader.ReadByte();
            }
        }

        private static void ReadGraphicsControlExtension(BinaryReader reader)
        {
            byte blockSize = reader.ReadByte();
            if (blockSize != 4)
            {
                throw new Exception("The block size for graphics control extension is not 4.  Something's wrong.");
            }

            //Bit 0  	Transparent Color Flag
            //Bit 1 	User Input Flag
            //Bits 2-4 	Disposal Method
            //Bits 5-7 	Reserved
            byte packed = reader.ReadByte();
            mGifInfo.HasTransparency = (packed & 1) == 1;


            Int16 delayTime = reader.ReadInt16();
            mGifInfo.DelayTimes.Add(delayTime);

            byte transparentColorIndex = reader.ReadByte();
            byte terminator = reader.ReadByte();

            mGifInfo.TransparentIndex = transparentColorIndex;

        }

        private static void ReadImageDescriptor(BinaryReader reader)
        {
            mGifInfo.CurrentBlockLeft = reader.ReadInt16();
            mGifInfo.CurrentBlockTop = reader.ReadInt16();
            mGifInfo.CurrentBlockWidth = reader.ReadInt16();
            mGifInfo.CurrentBlockHeight = reader.ReadInt16();

            Byte packed = reader.ReadByte();

            if (packed != 0)
            {
                mGifInfo.IsInterlaced = (packed & 1) == 1;

                bool sorted = (packed & 2) == 2;

                int sizeofLocalColorTableEntry = packed >> 5;

                if (sizeofLocalColorTableEntry != 0)
                {
                    throw new Exception("The GIF has a local color entry.  That's currently not supported");
                }

                mGifInfo.IsInterlaced = true;/*   
                throw new InvalidDataException(
                    "The image descriptor indicates the gif is interlaced, sorted, or uses a local color table.  Not currently supported.");*/
            }

            mGifInfo.LzwMin = reader.ReadByte();


            int currentEntrySize = mGifInfo.LzwMin + 1;

            FillDictionary();

            // TODO:  Fill with number of pixels to reduce GC
            mUncompressedColorIndexBuffer = new List<byte>();
            bool firstRead = true;

            MemoryStream memoryStream = new MemoryStream();

            byte dataBlockSize = reader.ReadByte();
            while (dataBlockSize != 0)
            {
                byte[] data = reader.ReadBytes(dataBlockSize);

                memoryStream.Write(data, 0, data.Length);

                dataBlockSize = reader.ReadByte();

            }

            FillUncompressedColorIndexBuffer(memoryStream.ToArray(), ref currentEntrySize, ref firstRead);

            

        }

        private static void TryReadGlobalColorTable(BinaryReader reader)
        {
            if (mGifInfo.HasGlobalColorTable)
            {
                mGifInfo.PaletteInfo = new PaletteInfo();
                mGifInfo.PaletteInfo.Entries = new RGB[mGifInfo.NumberOfColorEntries];

                for (int i = 0; i < mGifInfo.NumberOfColorEntries; i++)
                {
                    mGifInfo.PaletteInfo.Entries[i].R = reader.ReadByte();
                    mGifInfo.PaletteInfo.Entries[i].G = reader.ReadByte();
                    mGifInfo.PaletteInfo.Entries[i].B = reader.ReadByte();

                    mGifInfo.PaletteInfo.Entries[i].A = 255;// Default to opaque for now
                }
            }
        }

        #endregion

        #endregion
    }

    #region BitReader

    class BitReader
    {
        int mByteIndex = 0;
        int mBitIndex = 0;
        byte[] mBuffer;

        public int BitPosition
        {
            get
            {
                return mBitIndex + mByteIndex * 8;
            }
        }

        public int BitLength
        {
            get
            {
                return mBuffer.Length * 8;
            }
        }

        public BitReader(byte[] buffer)
        {
            mBuffer = buffer;
        }

        public int Read(int bits)
        {
            const int maximumSize = 32;

            if (bits > maximumSize)
            {
                throw new ArgumentException("Too many bits for BitReader");
            }
            if ((this.BitPosition + bits) > this.BitLength)
            {
                throw new InvalidOperationException("Reading past the end of the buffer.");
            }

            int result = 0;
            int bitsRead = 0;
            while (bits > 0)
            {
                int bitsToRead = System.Math.Min(bits, 8 - mBitIndex); // only read up to 8 bits at a time
                result += ((mBuffer[mByteIndex] >> mBitIndex) & ((1 << bitsToRead) - 1)) << bitsRead;

                bits -= bitsToRead;
                bitsRead += bitsToRead;
                mBitIndex += bitsToRead;
                if (mBitIndex >= 8)
                {
                    mBitIndex = 0;
                    ++mByteIndex;
                }
            }

            return result;
        }
    }

    #endregion
}
