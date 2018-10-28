using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            set
            {
                LastSelectedTexturePath = SelectedTextureFilePath;
                Set(value);
            }
        }

        public FilePath LastSelectedTexturePath
        {
            get { return Get<FilePath>(); }
            set { Set(value); }
        }

        public bool IsMagicWandSelected
        {
            get { return Get<bool>(); }
            set
            {
                Set(value);
            }
        }

        public bool IsSnapToGridChecked
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(IsSnapToGridChecked))]
        public bool IsGridSizeBoxEnabled
        {
            get
            {
                return IsSnapToGridChecked;
            }
        }
        public int GridSize
        {
            get { return Get<int>(); }
            set { Set(value); }
        }

        public ObservableCollection<FilePath> AvailableTextures
        {
            get; private set;
        } = new ObservableCollection<FilePath>();

        public WireframeEditControlsViewModel()
        {
            GridSize = 16;

        }

    }
}
