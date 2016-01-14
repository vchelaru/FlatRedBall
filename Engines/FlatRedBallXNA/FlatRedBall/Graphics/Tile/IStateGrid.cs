using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.ManagedSpriteGroups;

namespace FlatRedBall.Graphics.Tile
{
    public interface IStateGrid<T>
    {
        int Width
        {
            get;
        }

        int Height
        {
            get;
        }

        T GetStateAtPosition(int x, int y);

        bool IsGridPatternAt(int x, int y, GridPattern<T> pattern);

        List<GridRelativeState<T>> AsGridRelativeStateList();
        List<GridRelativeState<T>> AsGridRelativeStateList(int xOffset, int yOffset);
    }


    public static class IStateGridHelpers
    {

        public static List<GridRelativeState<T>> AsGridRelativeStateList<T>(IStateGrid<T> stateGrid)
        {
            return AsGridRelativeStateList<T>(stateGrid, 0, 0);
        }

        public static List<GridRelativeState<T>> AsGridRelativeStateList<T>(IStateGrid<T> stateGrid, int xOffset, int yOffset)
        {
            List<GridRelativeState<T>> toReturn = new List<GridRelativeState<T>>();

            for(int x = 0; x < stateGrid.Width; x++)
            {
                for (int y = 0; y < stateGrid.Height; y++)
                {
                    GridRelativeState<T> state = new GridRelativeState<T>(
                        x + xOffset, 
                        y + yOffset, 
                        stateGrid.GetStateAtPosition(x, y));

                    toReturn.Add(state);
                }
            }

            return toReturn;
        }

        public static bool IsGridPatternAt<T>(IStateGrid<T> stateGrid, int x, int y, GridPattern<T> pattern)
        {
            int stateGridWidth = stateGrid.Width;
            int stateGridHeight = stateGrid.Height;

            foreach (GridRelativeState<T> include in pattern.mIncludePattern)
            {
                // included are required, so let's fail if an included is not here
                if (x + include.X < 0 || y + include.Y < 0 || x + include.X >= stateGridWidth || y + include.Y >= stateGridHeight)
                {
                    return false;
                }
                T stateAtPosition = stateGrid.GetStateAtPosition(x + include.X, y + include.Y);
                if (!stateAtPosition.Equals(include.State))
                    return false;
            }

            foreach (GridRelativeStateList<T> orInclude in pattern.mOrIncludePattern)
            {

                if (x + orInclude.X < 0 || y + orInclude.Y < 0 || x + orInclude.X >= stateGridWidth || y + orInclude.Y >= stateGridHeight)
                {
                    return false;
                }

                T textureAtLocation = stateGrid.GetStateAtPosition(
                    x + orInclude.X,
                    y + orInclude.Y);

                foreach (T state in orInclude.StateList)
                {
                    if (state.Equals(textureAtLocation))
                        continue;
                }

                // If execution gets here then that means
                // a texture didn't match one of the textures
                // in the orinclude.
                return false;

            }

            foreach (GridRelativeState<T> exclude in pattern.mExcludePattern)
            {
                // The exclude pattern just says "it shouldn't be this", so anything outside of the bounds
                // has nothing (unless we want to change this later) so just continue on these.
                if (x + exclude.X < 0 || y + exclude.Y < 0 || x + exclude.X >= stateGridWidth || y + exclude.Y >= stateGridHeight)
                {
                    continue;
                }

                if (stateGrid.GetStateAtPosition(x + exclude.X, y + exclude.Y).Equals(exclude.State))
                    return false;

            }

            // If the function hasn't returned yet that means
            // that all textures matched.
            return true;
        }
    }
}
