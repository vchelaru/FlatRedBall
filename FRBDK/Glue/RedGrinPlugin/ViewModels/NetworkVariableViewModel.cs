using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedGrinPlugin.ViewModels
{
    public class NetworkVariableViewModel : ViewModel
    {
        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public bool IsChecked
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
    }
}
