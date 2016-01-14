using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.AnimationEditorForms.Data
{
    public class TileMapInformationList
    {
        public List<TileMapInformation> TileMapInfos = new List<TileMapInformation>();


        public TileMapInformation GetTileMapInformation(string fileName)
        {
            foreach(var info in TileMapInfos)
            {
                if(info.Name == fileName)
                {
                    return info;
                }
            }
            return null;
        }
    }
}
