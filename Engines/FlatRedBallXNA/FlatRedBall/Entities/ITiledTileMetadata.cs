using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Entities {
    
    public interface ITiledTileMetadata {
        void TileTexturePixelsSet(float LeftTextureCoordinate, float TopTextureCoordinate, float RightTextureCoordinate, float BottomTextureCoordinate);
    }

}
