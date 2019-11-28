using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.ViewModels
{
    public class AnimationRowViewModel
    {
        public ObservableCollection<AnimationSetViewModel> Animations { get; private set; }
            = new ObservableCollection<AnimationSetViewModel>();
    }
}
