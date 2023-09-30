using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.AI.LineOfSight
{
    internal class ViewerInformation
    {
        /// <summary>
        /// The last X position of the viewer on the visibility grid when the line of sight values were updated. This is used
        /// to check if line of sight values need to be updated.
        /// </summary>
        public int LastX
        {
            get;
            set;
        }

        /// <summary>
        /// The last Y position of the viewer on the visibility grid when the line of sight values were updated. This is used
        /// to check if line of sight values need to be updated.
        /// </summary>
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
