using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.RuntimeDebuggingPlugin.ViewModels
{
    public class MainViewModel : ViewModel
    {
        public bool ShowRuntimeControls
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

    }
}
