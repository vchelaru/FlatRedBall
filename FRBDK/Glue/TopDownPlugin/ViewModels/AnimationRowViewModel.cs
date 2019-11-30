using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.ViewModels
{
    public class AnimationRowViewModel : ViewModel
    {
        public ObservableCollection<AnimationSetViewModel> Animations { get; private set; }
            = new ObservableCollection<AnimationSetViewModel>();

        public string AnimationRowName
        {
            get => Get<string>();
            set => Set(value);
        }
    }
}
