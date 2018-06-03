using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.StateDataPlugin.ViewModels
{
    class StateViewModel : ViewModel
    {
        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public List<object> Variables
        {
            get { return Get<List<object>>(); }
            set { Set(value); }
        }


    }
}
