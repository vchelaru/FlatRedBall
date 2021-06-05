using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Tasks
{
    public class GlueTask
    {
        public DateTime TimeStarted { get; set;}
        public DateTime TimeEnded { get; set; }

        public Action Action
        {
            get;
            set;
        }

        public string DisplayInfo
        {
            get;
            set;
        }

        public string TaskType
        {
            get;
            set;
        }

        public bool DoOnUiThread { get; set; }
    }
}
