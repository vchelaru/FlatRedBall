using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FlatRedBall.Content;
using FlatRedBall;
using FlatRedBall.Content.Scene;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;




//////////////////////////////////
///////////////WARNING///////////
// This file comes from the 
// TMX Git repository by Domenic.
// If any changes are to be made, they
// should be made there then this file should
// be overwritten.
//////////////////////////////////



namespace TMXGlueLib.DataTypes
{

    public partial class ReducedQuadInfo
    {
        public float LeftQuadCoordinate;
        public float BottomQuadCorodinate;

        public ushort LeftTexturePixel;
        public ushort TopTexturePixel;

        public string Name;

        public static ReducedQuadInfo ReadFrom(BinaryReader reader)
        {
            ReducedQuadInfo toReturn = new ReducedQuadInfo();

            toReturn.LeftQuadCoordinate = reader.ReadSingle();
            toReturn.BottomQuadCorodinate = reader.ReadSingle();

            toReturn.LeftTexturePixel = reader.ReadUInt16();
            toReturn.TopTexturePixel = reader.ReadUInt16();

            toReturn.Name = reader.ReadString();

            return toReturn;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(LeftQuadCoordinate);
            writer.Write(BottomQuadCorodinate);
            writer.Write(LeftTexturePixel);
            writer.Write(TopTexturePixel);

            writer.Write(Name);
        }


        public override string ToString()
        {
            return Name + " " + LeftQuadCoordinate + " " + BottomQuadCorodinate;
        }
    }

    public class ReducedLayerInfo
    {
        public string Texture;       
 
        public uint NumberOfQuads;

        public List<ReducedQuadInfo> Quads = new List<ReducedQuadInfo>();

        public static ReducedLayerInfo ReadFrom(BinaryReader reader)
        {
            ReducedLayerInfo toReturn = new ReducedLayerInfo();


            toReturn.Texture = reader.ReadString();
            toReturn.NumberOfQuads = reader.ReadUInt32();

            for(int i = 0; i < toReturn.NumberOfQuads; i++)
            {
                toReturn.Quads.Add( ReducedQuadInfo.ReadFrom(reader));
            }

            return toReturn;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Texture);
            NumberOfQuads = (uint)Quads.Count;
            writer.Write(Quads.Count);

            for (int i = 0; i < NumberOfQuads; i++)
            {
                Quads[i].WriteTo(writer);
            }

        }

        public override string ToString()
        {
            return Texture + " (" + Quads.Count + ")";
        }
    }



    public partial class ReducedTileMapInfo
    {
        public ushort CellWidthInPixels;
        public ushort CellHeightInPixels;

        public float QuadWidth;
        public float QuadHeight;

        public uint NumberOfLayers;

        public List<ReducedLayerInfo> Layers = new List<ReducedLayerInfo>();

        public static ReducedTileMapInfo ReadFrom(BinaryReader reader)
        {
            ReducedTileMapInfo toReturn = new ReducedTileMapInfo();

            toReturn.CellWidthInPixels = reader.ReadUInt16();
            toReturn.CellHeightInPixels = reader.ReadUInt16();

            toReturn.QuadHeight = reader.ReadSingle();
            toReturn.QuadWidth = reader.ReadSingle();

            toReturn.NumberOfLayers = reader.ReadUInt32();

            for (int i = 0; i < toReturn.NumberOfLayers; i++)
            {
                toReturn.Layers.Add(ReducedLayerInfo.ReadFrom(reader));
            }


            return toReturn;
        }

        public void WriteTo(BinaryWriter writer)
        {
            NumberOfLayers = (uint)Layers.Count;

            writer.Write(CellWidthInPixels);
            writer.Write(CellHeightInPixels);

            writer.Write(QuadHeight);
            writer.Write(QuadWidth);

            writer.Write(NumberOfLayers);

            for (int i = 0; i < NumberOfLayers; i++)
            {
                this.Layers[i].WriteTo(writer);
            }
        }

        


        public override string ToString()
        {
            return this.Layers.Count.ToString();
        }
    }
}
