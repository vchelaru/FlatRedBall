using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics.Tile;

using FlatRedBall.ManagedSpriteGroups;


#if SILVERLIGHT

#else
using Color = Microsoft.Xna.Framework.Color;
#endif

namespace FlatRedBall.Graphics.Texture
{
    public partial class ImageData : IStateGrid<Color>, IStateGrid<byte>
    {
        #region IStateGrid<byte> Members

        byte IStateGrid<byte>.GetStateAtPosition(int x, int y)
        {
            return mByteData[x + y * Width];
        }

        List<GridRelativeState<byte>> IStateGrid<byte>.AsGridRelativeStateList()
        {
            return IStateGridHelpers.AsGridRelativeStateList<byte>(this);
        }

        List<GridRelativeState<byte>> IStateGrid<byte>.AsGridRelativeStateList(int xOffset, int yOffset)
        {
            return IStateGridHelpers.AsGridRelativeStateList<byte>(this, xOffset, yOffset);
        }

        bool IStateGrid<byte>.IsGridPatternAt(int x, int y, GridPattern<byte> pattern)
        {
            return IStateGridHelpers.IsGridPatternAt<byte>(this, x, y, pattern);
        }

        #endregion

        #region IStateGrid<Color> Members

        Color IStateGrid<Color>.GetStateAtPosition(int x, int y)
        {
            return mData[x + y * Width];
        }

        List<GridRelativeState<Color>> IStateGrid<Color>.AsGridRelativeStateList()
        {
            return IStateGridHelpers.AsGridRelativeStateList<Color>(this);
        }

        List<GridRelativeState<Color>> IStateGrid<Color>.AsGridRelativeStateList(int xOffset, int yOffset)
        {
            return IStateGridHelpers.AsGridRelativeStateList<Color>(this, xOffset, yOffset);
        }

        bool IStateGrid<Color>.IsGridPatternAt(int x, int y, GridPattern<Color> pattern)
        {
            return IStateGridHelpers.IsGridPatternAt<Color>(this, x, y, pattern);
        }

        #endregion

    }
}
