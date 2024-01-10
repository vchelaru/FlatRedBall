using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCommunicationPlugin.GlueControl.ViewModels
{
    public class ProfilingControlViewModel : ViewModel
    {
        public bool IsAutoSnapshotEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsGameRunning
        {
            get => Get<bool>();
            set => Set(value);
        }
        public string SummaryText
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CollisionText
        {
            get => Get<string>();
            set => Set(value);
        }
    }
}
