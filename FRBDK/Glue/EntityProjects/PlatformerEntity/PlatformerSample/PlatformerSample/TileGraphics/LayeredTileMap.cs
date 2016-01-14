using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content;
using FlatRedBall.IO;
using FlatRedBall.Debugging;
using FlatRedBall.Performance.Measurement;

namespace FlatRedBall.TileGraphics
{
    public class LayeredTileMap : PositionedObject
    {
        #region Fields

        List<MapDrawableBatch> mMapLists = new List<MapDrawableBatch>();

        float mRenderingScale = 1;

        float mZSplit = 1;

        #endregion

        #region Properties

        public float RenderingScale
        {
            get
            {
                return mRenderingScale;
            }
            set
            {
                mRenderingScale = value;
                foreach (var map in mMapLists)
                {
                    map.RenderingScale = value;
                }

            }
        }

        public float ZSplit
        {
            get
            {
                return mZSplit;
            }
            set
            {
                for (int i = 0; i < this.mMapLists.Count; i++)
                {
                    mMapLists[i].RelativeZ = mZSplit * i;
                }
            }
        }

        public List<MapDrawableBatch> MapLayers
        {
            get
            {
                return mMapLists;
            }
        }

        public bool Visible
        {
            set
            {
                foreach (var item in this.mMapLists)
                {
                    item.Visible = value;
                }
            }
        }


        #endregion

        public static LayeredTileMap FromScene(string fileName, string contentManagerName)
        {
            return FromScene(fileName, contentManagerName, false);
        }

        public static LayeredTileMap FromScene(string fileName, string contentManagerName, bool verifySameTexturePerLayer)
        {
            Section.GetAndStartContextAndTime("Initial FromScene");
            LayeredTileMap toReturn = new LayeredTileMap();

            string absoluteFileName = FileManager.MakeAbsolute(fileName);
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("FromFile");
            SpriteEditorScene ses = SpriteEditorScene.FromFile(absoluteFileName);
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("BreaksNStuff");

            string oldRelativeDirectory = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = FileManager.GetDirectory(absoluteFileName);

            var breaks = GetZBreaks(ses.SpriteList);

            int valueBefore = 0;

            MapDrawableBatch mdb;
            int valueAfter;

            float zValue = 0;
            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Create MDBs");

            for(int i = 0; i < breaks.Count; i++)
            {
                valueAfter = breaks[i];

                int count = valueAfter - valueBefore;

                mdb = MapDrawableBatch.FromSpriteSaves(ses.SpriteList, valueBefore, count, contentManagerName, verifySameTexturePerLayer);
                mdb.AttachTo(toReturn, false);
                mdb.RelativeZ = zValue;
                toReturn.mMapLists.Add(mdb);
                zValue += toReturn.mZSplit;
                valueBefore = valueAfter;
            }

            valueAfter = ses.SpriteList.Count;
            if (valueBefore != valueAfter)
            {
                int count = valueAfter - valueBefore;

                mdb = MapDrawableBatch.FromSpriteSaves(ses.SpriteList, valueBefore, count, contentManagerName, verifySameTexturePerLayer);
                mdb.AttachTo(toReturn, false);
                mdb.RelativeZ = zValue;

                toReturn.mMapLists.Add(mdb);
            }
            Section.EndContextAndTime();
            FileManager.RelativeDirectory = oldRelativeDirectory;
            return toReturn;
        }

        public void AddToManagers()
        {
            foreach (var item in this.mMapLists)
            {
                item.AddToManagers();
            }
        }

        
         //Write some addtomanagers and remove methods

        static List<int> GetZBreaks(List<SpriteSave> list)
        {
            List<int> zBreaks = new List<int>();

            GetZBreaks(list, zBreaks);

            return zBreaks;

        }

        static void GetZBreaks(List<SpriteSave> list, List<int> zBreaks)
        {
            zBreaks.Clear();

            if (list.Count == 0 || list.Count == 1)
                return;

            for (int i = 1; i < list.Count; i++)
            {
                if (list[i].Z != list[i - 1].Z)
                    zBreaks.Add(i);
            }
        }

        public void Destroy()
        {

            foreach (var layer in this.mMapLists)
            {
                SpriteManager.RemoveDrawableBatch(layer);
            }

        }
    }
}
