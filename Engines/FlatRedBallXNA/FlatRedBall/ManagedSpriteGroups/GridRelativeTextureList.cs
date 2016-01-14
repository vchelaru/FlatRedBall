using System;
using System.Collections.Generic;
using System.Text;

#if FRB_XNA || ZUNE || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.ManagedSpriteGroups
{
    public class GridRelativeStateList<T>
    {
        public int X;
        public int Y;

        public List<T> StateList = new List<T>();

        public IEnumerator<T> GetEnumerator()
        {
            return StateList.GetEnumerator();
        }

        public GridRelativeStateList(int x, int y)
        {
            Y = y;
            X = x;
        }

        public GridRelativeStateList<T> Clone()
        {
            GridRelativeStateList<T> toReturn = new GridRelativeStateList<T>(X, Y);

            foreach (T element in StateList)
            {
                toReturn.StateList.Add(element);
            }

            return toReturn;
        }
    }
}
