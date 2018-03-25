using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace FlatRedBall.AnimationEditorForms.ViewModels
{
    public class WireframeEditControlsViewModel : ViewModel
    {
        public FilePath SelectedTextureFilePath
        {
            get { return Get<FilePath>(); }
            set { Set(value); }
        }

        public bool IsMagicWandSelected
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public ObservableCollection<FilePath> AvailableTextures
        {
            get; private set;
        } = new ObservableCollection<FilePath>();

    }
}
