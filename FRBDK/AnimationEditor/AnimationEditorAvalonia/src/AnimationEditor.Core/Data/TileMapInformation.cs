using System.Collections.Generic;

namespace AnimationEditor.Core.Data
{
    public class TileMapInformation
    {
        public string Name;
        public int TileWidth;
        public int TileHeight;
    }

    public class TileMapInformationList
    {
        public List<TileMapInformation> TileMapInfos = new List<TileMapInformation>();

        public TileMapInformation GetTileMapInformation(string fileName)
        {
            foreach (var info in TileMapInfos)
            {
                if (info.Name == fileName)
                {
                    return info;
                }
            }
            return null;
        }
    }
}
