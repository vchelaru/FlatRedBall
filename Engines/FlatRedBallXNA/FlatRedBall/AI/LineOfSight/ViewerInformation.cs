using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.AI.LineOfSight
{
    internal class ViewerInformation
    {
        public int LastX
        {
            get;
            set;
        }

        public int LastY
        {
            get;
            set;
        }


        public VisibilityGrid LocalVisibilityGrid
        {
            get;
            set;
        }
    }
}
