using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Instructions.Reflection;

namespace EditingControls
{
    public class UndoState
    {
        public float? X
        {
            get;
            set;
        }

        public float? Y
        {
            get;
            set;
        }

        public float? Z
        {
            get;
            set;
        }

        public float? ScaleX
        {
            get;
            set;
        }

        public float? ScaleY
        {
            get;
            set;
        }


        public object Owner
        {
            get;
            set;

        }

        public void ApplyUndo()
        {
            var lateBinder = LateBinder.GetInstance(Owner.GetType());

            if (X.HasValue)
            {
                lateBinder.SetValue(Owner, "X", X.Value);
            }
            if (Y.HasValue)
            {
                lateBinder.SetValue(Owner, "Y", Y.Value);
            }
            if (Z.HasValue)
            {
                lateBinder.SetValue(Owner, "Z", Z.Value);
            }

            if (ScaleX.HasValue)
            {
                lateBinder.SetValue(Owner, "ScaleX", ScaleX.Value);
            }
            if (ScaleY.HasValue)
            {
                lateBinder.SetValue(Owner, "ScaleY", ScaleY.Value);
            }
        }

        public static UndoState FromObject(object recordFrom)
        {
            UndoState toReturn = new UndoState();
            toReturn.Owner = recordFrom;

            var lateBinder = LateBinder.GetInstance(recordFrom.GetType());

            object result;

            if(lateBinder.TryGetValue(recordFrom, "X", out result))
            {
                toReturn.X = (float)result;
            }
            if (lateBinder.TryGetValue(recordFrom, "Y", out result))
            {
                toReturn.Y = (float)result;
            }
            if (lateBinder.TryGetValue(recordFrom, "Z", out result))
            {
                toReturn.Z = (float)result;
            }

            if (lateBinder.TryGetValue(recordFrom, "ScaleX", out result))
            {
                toReturn.ScaleX = (float)result;
            }
            if (lateBinder.TryGetValue(recordFrom, "ScaleY", out result))
            {
                toReturn.ScaleY = (float)result;
            }

            return toReturn;
        }

        public UndoState GetDiff(object compareTo)
        {
            UndoState toReturn = new UndoState();
            var lateBinder = LateBinder.GetInstance(compareTo.GetType());
            object result = null;
            bool foundSomething = false;
            if (X.HasValue && lateBinder.TryGetValue(compareTo, "X", out result) && (float)result != X.Value)
            {
                toReturn.X = this.X;
                foundSomething = true;
            }
            if (Y.HasValue && lateBinder.TryGetValue(compareTo, "Y", out result) && (float)result != Y.Value)
            {
                toReturn.Y = this.Y;
                foundSomething = true;
            }
            if (Z.HasValue &&  lateBinder.TryGetValue(compareTo, "Z", out result) && (float)result != Z)
            {
                toReturn.Z = this.Z;
                foundSomething = true;
            }

            if (ScaleX.HasValue && lateBinder.TryGetValue(compareTo, "ScaleX", out result) && (float)result != ScaleX)
            {
                toReturn.ScaleX = this.ScaleX;
                foundSomething = true;
            }
            if (ScaleY.HasValue && lateBinder.TryGetValue(compareTo, "ScaleY", out result) && (float)result != ScaleY)
            {
                toReturn.ScaleY = this.ScaleY;
                foundSomething = true;
            }

            if (foundSomething)
            {
                toReturn.Owner = compareTo;
                return toReturn;
            }
            else
            {
                return null;
            }
        }
    }
}
