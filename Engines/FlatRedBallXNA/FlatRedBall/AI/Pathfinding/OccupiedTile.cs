using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.AI.Pathfinding
{
    internal class OccupiedTile
    {
        public int X
        {
            get;
            set;
        }

        public int Y
        {
            get;
            set;
        }

        public object Occupier
        {
            get;
            set;
        }

    }
}
