using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.ViewModels
{
    public class AllAnimationValuesViewModel : ViewModel
    {
        public ObservableCollection<AnimationRowViewModel> AnimationRows { get; private set; } =
            new ObservableCollection<AnimationRowViewModel>();
    }
}
