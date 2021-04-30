using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TMXGlueLib
{
    public static class TilesetExtensionMethods
    {
        public static void IndexToCoordinate(this Tileset tileset, long xIndex, long yIndex, out int xCoordinate, out int yCoordinate)
        {
            xCoordinate = tileset.Margin + (int)xIndex * (tileset.Tilewidth + tileset.Spacing);
            yCoordinate = tileset.Margin + (int)yIndex * (tileset.Tileheight + tileset.Spacing);


        }

        public static void CoordinateToIndex(this Tileset tileset, int xCoordinate, int yCoordinate, out int xIndex, out int yIndex)
        {
            xIndex = 0;
            yIndex = 0;
            if (tileset.Images.Length != 0)
            {
                // We're assuming the first image, not sure why we'd have multiple images in one tileset....or at least we won't 
                // supportthat yet.
                var image = tileset.Images[0];

                int effectiveImageWidth = tileset.Images[0].width;
                int effectiveImageHeight = tileset.Images[0].height;

                if (xCoordinate < effectiveImageWidth && yCoordinate < effectiveImageHeight)
                {


                    if (tileset.Margin != 0)
                    {
                        xCoordinate -= tileset.Margin * 2;
                        yCoordinate -= tileset.Margin * 2;
                    }


                    int effectiveTileWidth = tileset.Tilewidth + tileset.Spacing;
                    int effectiveTileHeight = tileset.Tileheight + tileset.Spacing;

                    xIndex = xCoordinate / effectiveTileWidth;
                    yIndex = yCoordinate / effectiveTileHeight;

                }

            }
        }

        public static int GetNumberOfTilesWide(this Tileset tileset)
        {
            if (tileset.Images.Length == 0 || tileset.Tilewidth == 0)
            {
                return 0;
            }
            else
            {
                // This is the width of the image as reported by the .tsx or .tmx, but
                // it may not actually be the image's width if the .png has changed since
                // the tsx/tmx was saved
                int imageWidth = tileset.Images[0].width;

                return GetNumberOfTilesWide(
                    tileset.Images[0].width, tileset.Margin, tileset.Tilewidth, tileset.Spacing);
            }
        }


        public static int GetNumberOfTilesWide(int imageWidth, int margin, int tileWidth, int spacing)
        {

            if (tileWidth == 0)
            {
                throw new Exception("The tileWidth must not be 0");
            }


            // The following logic
            // deserves an explanation:
            // Consider a simple tileset
            // with 2 tiles, and a single
            // spacing between them.  Assume
            // that the tiles are 16 pixels wide
            // and that the spacing is 1 pixel wide.
            // In this case, the width of the entire
            // tileset image would be 33.  That's (16+1+16).
            // However, let's look at the following line of code
            // below:
            //int tilesWide = (imageWidth - margin) / (tileWidth + spacing);
            // In this case, the formula would be:
            // int tilesWide = (33-0) / (16+1);
            // Which is equivalent to:
            // int tilesWide = 33 / 17;
            // which is equivalent to:
            // int tilesWide = 1;
            // But we clearly stated above that we have two tiles (16+1+16).
            // The reason for this is because each tile *except the last* will
            // be considered to be wider by 1 because of its spacing value.  The
            // last one won't be considered wider because there's always one-less space
            // than there are tiles.
            // We can correct this simply by adding 1 to the resulting tilesWide *if* there
            // is spacing...
            // Update January 26, 2014
            // We need to multiply margin
            // by 2 since the margin applies
            // to all sides
            // ...so let's do that here:
            //int tilesWide = (imageWidth - (2 * margin)) / (tileWidth + spacing);

            //if (spacing != 0)
            //{
            //    tilesWide++;
            //}

            // Update February 7, 2014
            // No, this doesn't seem like it's working right, and it's confusing, so I'm going
            // to break this down a bit:
            // First let's take off the margin so we can see how
            // much usable space we have:
            int usableSpace = imageWidth - 2 * margin;
            // If there is a margin, all tiles except the last will have the margin added to the right of them
            // Since the last one won't have the margin added, let's just add the margin to the usable space, then do 
            // a simple int division:
            usableSpace += spacing;

            return usableSpace / (tileWidth + spacing);

        }

        public static int GetTileCount(this Tileset tileset)
        {
            int width = tileset.GetNumberOfTilesWide();
            int height = tileset.GetNumberOfTilesTall();

            return width * height;


        }

        public static int GetNumberOfTilesTall(this Tileset tileset)
        {
            if (tileset.Images.Length == 0)
            {
                return 0;
            }
            else
            {
                return GetNumberOfTilesWide(
                    tileset.Images[0].height, tileset.Margin, tileset.Tileheight, tileset.Spacing);
            }
        }


        public static int GetNumberOfTilesTall(int imageHeight, int margin, int tileHeight, int spacing)
        {
            // See GetNumberOfTilesWide for a discussion about this approach
            int usableSpace = imageHeight - 2 * margin;
            usableSpace += spacing;

            return usableSpace / (tileHeight + spacing);


        }

        public static int IndexToLocalId(this Tileset tileset, int xIndex, int yIndex)
        {
            return xIndex + yIndex * tileset.GetNumberOfTilesWide();

        }

        public static int CoordinateToLocalId(this Tileset tileset, int xCoordinate, int yCoordinate)
        {
            int xIndex;
            int yIndex;

            tileset.CoordinateToIndex(xCoordinate, yCoordinate, out xIndex, out yIndex);

            return tileset.IndexToLocalId(xIndex, yIndex);

        }

    }
}
;