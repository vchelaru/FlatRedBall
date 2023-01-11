using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Entities {

    public struct TiledTileMetadata {
        public float LeftTextureCoordinate, TopTextureCoordinate, RightTextureCoordinate, BottomTextureCoordinate;
    }

    public interface ITiledTileMetadata {
        void SetTileMetadata(TiledTileMetadata tileMetadata);
    }

}
