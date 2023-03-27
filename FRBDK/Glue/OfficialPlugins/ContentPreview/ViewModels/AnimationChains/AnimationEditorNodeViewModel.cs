using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ContentPreview.ViewModels.AnimationChains
{
    public class AnimationEditorNodeViewModel : ViewModel
    {
        public string Text
        {
            get => Get<string>();
            set => Set(value);
        }

        public ObservableCollection<AnimationEditorNodeViewModel> VisibleChildren { get; set; } = new ObservableCollection<AnimationEditorNodeViewModel>();

        public override string ToString() => Text;
    }
}
