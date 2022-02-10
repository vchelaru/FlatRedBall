using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public abstract Task DoAction();
    }

    public class GlueTask : GlueTaskBase
    {
        public Action Action { get; set; }

        public override Task DoAction()
        {
            if (DoOnUiThread)
            {
                global::Glue.MainGlueWindow.Self.Invoke(Action);
            }
            else
            {
                Action();
            }

            return Task.CompletedTask;
        }
    }

    public class GlueTask<T> : GlueTaskBase
    {
        public Func<T> Func { get; set; }

        public override Task DoAction()
        {
            if (DoOnUiThread)
            {
                Result = global::Glue.MainGlueWindow.Self.Invoke(() => Result = Func());
            }
            else
            {
                Result = Func();

            }

            return Task.CompletedTask;
        }
    }

    public class GlueAsyncTask : GlueTaskBase
    {
        public Func<Task> Func { get; set; }

        public override async Task DoAction()
        {
            if (DoOnUiThread)
            {
                await global::Glue.MainGlueWindow.Self.Invoke(() => Func());
            }
            else
            {
                await Func();

            }
        }
    }

    public class GlueAsyncTask<T> : GlueTaskBase
    {
        public Func<Task<T>> Func { get; set; }

        public override async Task DoAction()
        {
            if (DoOnUiThread)
            {
                Result = await global::Glue.MainGlueWindow.Self.Invoke(() => Func());
            }
            else
            {
                Result = await Func();

            }
        }
    }
}
