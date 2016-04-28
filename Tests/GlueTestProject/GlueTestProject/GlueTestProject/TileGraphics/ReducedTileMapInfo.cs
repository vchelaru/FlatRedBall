using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using FlatRedBall.Content;
using FlatRedBall;
using FlatRedBall.Content.Scene;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;


namespace TMXGlueLib.DataTypes
{
    #region ReducedQuadInfo

    public partial class ReducedQuadInfo
    {
        public const byte FlippedHorizontallyFlag = 8;
        public const byte FlippedVerticallyFlag = 4;
        public const byte FlippedDiagonallyFlag = 2;

        public float LeftQuadCoordinate;
        public float BottomQuadCoordinate;

        public ushort LeftTexturePixel;
        public ushort TopTexturePixel;

        public byte FlipFlags;

        public string Name;

        public static ReducedQuadInfo ReadFrom(BinaryReader reader)
        {
            ReducedQuadInfo toReturn = new ReducedQuadInfo();

            toReturn.LeftQuadCoordinate = reader.ReadSingle();
            toReturn.BottomQuadCoordinate = reader.ReadSingle();
            toReturn.LeftTexturePixel = reader.ReadUInt16();
            toReturn.TopTexturePixel = reader.ReadUInt16();

            toReturn.Name = reader.ReadString();

            toReturn.FlipFlags = reader.ReadByte();

            return toReturn;
        }


        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(LeftQuadCoordinate);
            writer.Write(BottomQuadCoordinate);
            writer.Write(LeftTexturePixel);
            writer.Write(TopTexturePixel);

            writer.Write(Name);

            writer.Write(FlipFlags);
        }


        public override string ToString()
        {
            return Name + " " + LeftQuadCoordinate + " " + BottomQuadCoordinate;
        }
    }

    #endregion

    #region ReducedLayerInfo

    public class ReducedLayerInfo
    {
        public string Texture;
        public string Name;

        public uint NumberOfQuads;

        public float Z;

        public List<ReducedQuadInfo> Quads = new List<ReducedQuadInfo>();

        // Version 2:
        public int TextureId;


        public static ReducedLayerInfo ReadFrom(BinaryReader reader, int version)
        {
            ReducedLayerInfo toReturn = new ReducedLayerInfo();

            toReturn.Z = reader.ReadSingle();

            toReturn.Texture = reader.ReadString();

            toReturn.Name = reader.ReadString();

            toReturn.NumberOfQuads = reader.ReadUInt32();

            for(int i = 0; i < toReturn.NumberOfQuads; i++)
            {
                toReturn.Quads.Add( ReducedQuadInfo.ReadFrom(reader));
            }

            if(version >= 2)
            {
                toReturn.TextureId = reader.ReadInt32();
            }

            return toReturn;
        }

        public void WriteTo(BinaryWriter writer, int version)
        {
            writer.Write(Z);

            writer.Write(Texture);

            writer.Write(Name);
            
            NumberOfQuads = (uint)Quads.Count;
            writer.Write(Quads.Count);

            for (int i = 0; i < NumberOfQuads; i++)
            {
                Quads[i].WriteTo(writer);
            }

            if (version >= 2)
            {
                writer.Write(TextureId);
            }
        }

        public override string ToString()
        {
            return Texture + " (" + Quads.Count + ")";
        }
    }

    #endregion

    #region ReducedTileMapInfo


    public partial class ReducedTileMapInfo
    {
        public ushort CellWidthInPixels;
        public ushort CellHeightInPixels;

        public float QuadWidth;
        public float QuadHeight;

        public uint NumberOfLayers;


        // Version 0:
        // Initial version when versioning was tracked.
        // Version 1:
        // Added:
        //  int NumberCellsWide;
        //  int NumberCellsTall;
        public int VersionNumber = 2;

        public int NumberCellsWide;
        public int NumberCellsTall;

        public List<ReducedLayerInfo> Layers = new List<ReducedLayerInfo>();

        public static ReducedTileMapInfo ReadFrom(BinaryReader reader)
        {
            ReducedTileMapInfo toReturn = new ReducedTileMapInfo();

            toReturn.VersionNumber = reader.ReadInt32();

            toReturn.CellWidthInPixels = reader.ReadUInt16();
            toReturn.CellHeightInPixels = reader.ReadUInt16();

            toReturn.QuadHeight = reader.ReadSingle();
            toReturn.QuadWidth = reader.ReadSingle();

            toReturn.NumberOfLayers = reader.ReadUInt32();

            for (int i = 0; i < toReturn.NumberOfLayers; i++)
            {

                toReturn.Layers.Add(ReducedLayerInfo.ReadFrom(reader, toReturn.VersionNumber));
            }

            // Version 1:
            if(toReturn.VersionNumber > 0)
            {
                toReturn.NumberCellsWide = reader.ReadInt32();
                toReturn.NumberCellsTall = reader.ReadInt32();
            }


            return toReturn;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(VersionNumber);

            writer.Write(CellWidthInPixels);
            writer.Write(CellHeightInPixels);

            writer.Write(QuadHeight);
            writer.Write(QuadWidth);

            NumberOfLayers = (uint)Layers.Count;
            writer.Write(NumberOfLayers);

            for (int i = 0; i < NumberOfLayers; i++)
            {
                this.Layers[i].WriteTo(writer, VersionNumber);
            }

            // Version 1:
            if(VersionNumber > 0)
            {
                writer.Write(NumberCellsWide);
                writer.Write(NumberCellsTall);
            }

        }

        public static ReducedTileMapInfo FromFile(string fileName)
        {
            ReducedTileMapInfo rtmi = null;
            using (Stream inputStream = FileManager.GetStreamForFile(fileName))
            using (BinaryReader binaryReader = new BinaryReader(inputStream))
            {
                rtmi = ReducedTileMapInfo.ReadFrom(binaryReader);

            }

            return rtmi;
        }
        public override string ToString()
        {
            return this.Layers.Count.ToString(CultureInfo.InvariantCulture);
        }

        public List<string> GetReferencedFiles()
        {
            List<string> toReturn = new List<string>();

            foreach (var item in Layers)
            {
                toReturn.Add(item.Texture);

            }

            return toReturn;
        }
    }

    #endregion
}
