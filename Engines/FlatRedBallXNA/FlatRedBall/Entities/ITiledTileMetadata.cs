using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Entities {
    
    public interface ITiledTileMetadata {
        int TileLeftTexturePixel { get; set; }
        int TileTopTexturePixel { get; set; }
        int TileTexturePixelSize { get; set; }
        void TileTexturePixelsSet();
    }

}
