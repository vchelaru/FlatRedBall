using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMXGlueLib;

namespace TmxEditor.Managers
{
    /// <summary>
    /// Responsible for fixing tilesets when the underlying image is resized.
    /// </summary>
    public class ResizeFixer
    {
        public void FixTileset(Tileset tileset, int actualTextureWith)
        {

            int widthAccordingToTileset = tileset.Images[0].width;
            

            if(widthAccordingToTileset != actualTextureWith)
            {
                int rowsAccordingToTileset = tileset.GetNumberOfTilesWide();
                int rowsForImage = TilesetExtensionMethods.GetNumberOfTilesWide(
                    actualTextureWith, tileset.Margin, tileset.Tilewidth, tileset.Spacing);

                var list = tileset.TileDictionary.ToList();

                foreach(var kvp in list)
                {
                    uint oldId = kvp.Key;

                    uint oldRow = (uint)(oldId / rowsAccordingToTileset);
                    uint oldColumn = (uint)(oldId % rowsAccordingToTileset);


                    // todo - assign new column, eventually save this thing out

                }
            }
        }
    }
}
