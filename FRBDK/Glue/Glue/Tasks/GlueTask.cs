using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Tasks
{
    public abstract class GlueTaskBase
    {
        public DateTime TimeStarted { get; set;}
        public DateTime TimeEnded { get; set; }

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

        public TaskExecutionPreference TaskExecutionPreference { get; set; }

        public bool DoOnUiThread { get; set; }

        public override string ToString() => DisplayInfo;

        public object Result { get; set; }

        public abstract void DoAction();
    }

    public class GlueTask : GlueTaskBase
    {
        public Action Action { get; set; }

        public override void DoAction()
        {
            if (DoOnUiThread)
            {
                global::Glue.MainGlueWindow.Self.Invoke(Action);
            }
            else
            {
                Action();

            }
        }
    }

    public class GlueTask<T> : GlueTaskBase
    {
        public Func<T> Func { get; set; }

        public override void DoAction()
        {
            if (DoOnUiThread)
            {
                Result = global::Glue.MainGlueWindow.Self.Invoke(() => Result = Func());
            }
            else
            {
                Result = Func();

            }
        }
    }
}
