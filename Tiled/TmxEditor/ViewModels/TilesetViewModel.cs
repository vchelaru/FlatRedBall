using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TmxEditor.ViewModels
{
    public class TilesetViewModel
    {
        int mTileWidth = 16;
        int mTileHeight = 16;
        int mMargin = 0;
        int mSpacing = 0;
        bool mCopyFile = true;

        public string Name { get; set; }
        public int TileWidth 
        {
            get { return mTileWidth; }
            set 
            { 
                mTileWidth = value;
                mTileWidth = Math.Max(1, mTileWidth);
            }
        }
        public int TileHeight 
        {
            get { return mTileHeight; }
            set 
            { 
                mTileHeight = value;
                mTileHeight = Math.Max(1, mTileHeight);

            }
        }
        public int Margin
        {
            get { return mMargin; }
            set 
            { 
                mMargin = value;
                mMargin = Math.Max(0, mMargin);

            }
        }
        public int Spacing 
        {
            get { return mSpacing; }
            set 
            { 
                mSpacing = value;
                mSpacing = Math.Max(0, mSpacing);

            }
        }

        public bool CopyFile
        {
            get
            {
                return mCopyFile;
            }
            set
            {
                mCopyFile = value;
            }
        }
    }
}
