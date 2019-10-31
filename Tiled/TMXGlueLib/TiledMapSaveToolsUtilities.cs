using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TMXGlueLib
{
    /// <summary>
    /// Contains utility methods specific to tools - these should not be needed at runtime (in games)
    /// </summary>
    public static class TiledMapSaveToolsUtilities
    {

        /// <summary>
        /// Sets all Image width/height values inside all Tilesets according to actual
        /// texture sizes.  The reason this is needed is because the user could have chnaged
        /// the size of the images (like resized a PNG) and not opened/resaved the file in Tiled.
        /// Therefore, the image size may be incorrect in the TMX.
        /// </summary>
        /// <param name="directory">The directory of the TMX, which is used to load images which typically use relative paths.</param>
        public static void CorrectImageSizes(this TiledMapSave tiledMapSave, string directory)
        {
            foreach (var tileset in tiledMapSave.Tilesets)
            {
                foreach (var image in tileset.Images)
                {
                    string absolutePath = image.Source;

                    if (FileManager.IsRelative(absolutePath))
                    {
                        absolutePath = directory + absolutePath;
                    }

                    if (System.IO.File.Exists(absolutePath))
                    {
                        var dimensions = ImageHeader.GetDimensions(absolutePath);

                        image.width = dimensions.Width;
                        image.height = dimensions.Height;
                    }
                }
            }
        }

    }
}
