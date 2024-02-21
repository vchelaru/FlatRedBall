using FlatRedBall.Glue.Managers;
using GlueFormsCore.Controls;
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

        public string CustomId
        {
            get; set;
        }

        public string EffectiveId => CustomId ?? DisplayInfo;

        public string TaskType
        {
            get;
            set;
        }

        public TaskExecutionPreference TaskExecutionPreference { get; set; }

        public bool DoOnUiThread { get; set; }

        public override string ToString()
        {
            if (!IsCancelled)
            {
                return DisplayInfo;
            }
            else
            {
                return $"Cancelled {DisplayInfo}";
            }
        }

        public object Result { get; set; }

        public bool IsCancelled { get; set; }

        // Named in a very particular way so that it can be checked
        // in the TaskManager and not have any name collision.
        internal abstract Task Do_Action_Internal();
    }

    public class GlueTask : GlueTaskBase
    {
        public Action Action { get; set; }

        internal override Task Do_Action_Internal()
        {
            if (DoOnUiThread && !TaskManager.Self.IsOnUiThread)
            {
                MainPanelControl.Self.Invoke(Action);
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

        internal override Task Do_Action_Internal()
        {
            if (DoOnUiThread && !TaskManager.Self.IsOnUiThread)
            {
                Result = MainPanelControl.Self.Invoke(() => Result = Func());
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

        internal override async Task Do_Action_Internal()
        {
            if (DoOnUiThread && !TaskManager.Self.IsOnUiThread)
            {
                await MainPanelControl.Self.Invoke(() => Func());
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

        internal override async Task Do_Action_Internal()
        {
            if (DoOnUiThread && !TaskManager.Self.IsOnUiThread)
            {
                Result = MainPanelControl.Self.Invoke(() => Func());
            }
            else
            {
                Result = await Func();

            }
        }
    }
}
