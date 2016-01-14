using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Graphics.Tile
{
    public class GridRelativeState<T>
    {
        #region XML Docs
        /// <summary>
        /// The relative X position.
        /// </summary>
        #endregion
        public int X;

        #region XML Docs
        /// <summary>
        /// The relative Y position.
        /// </summary>
        #endregion
        public int Y;

        public T State;

        public GridRelativeState(int x, int y, T state)
        {
            X = x;
            Y = y;
            State = state;
        }

        public GridRelativeState<T> Clone()
        {
            GridRelativeState<T> toReturn = new GridRelativeState<T>(X, Y, State);

            return toReturn;
        }

    }
}
