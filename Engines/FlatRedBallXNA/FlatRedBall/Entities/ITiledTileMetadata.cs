using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Entities {
    
    public interface ITiledTileMetadata {
        int Tile_LeftTexturePixel { get; set; }
        int Tile_TopTexturePixel { get; set; }
        int Tile_TexturePixelSize { get; set; }
        //public void Tile_TexturePixelsSet();
    }

}
