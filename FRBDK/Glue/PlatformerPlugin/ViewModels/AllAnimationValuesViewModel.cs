using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace PlatformerPlugin.ViewModels
{
    public class AllAnimationValuesViewModel : ViewModel
    {
        public ObservableCollection<AnimationRowViewModel> AnimationRows { get; private set; } =
            new ObservableCollection<AnimationRowViewModel>();
    }
}
