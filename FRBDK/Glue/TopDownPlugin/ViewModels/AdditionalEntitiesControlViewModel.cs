using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.ViewModels
{
    class AdditionalEntitiesControlViewModel : ViewModel
    {
        public bool IsTopDownEntity
        {
            get => Get<bool>();
            set => Set(value);
        }
    }
}
