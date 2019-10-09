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
            get => Get<FilePath>();
            set
            {
                LastSelectedTexturePath = SelectedTextureFilePath;
                Set(value);
            }
        }

        public FilePath LastSelectedTexturePath
        {
            get => Get<FilePath>();
            set => Set(value); 
        }

        public bool IsMagicWandSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSnapToGridChecked
        {
            get => Get<bool>(); 
            set => Set(value);
        }

        [DependsOn(nameof(IsSnapToGridChecked))]
        public bool IsGridSizeBoxEnabled => IsSnapToGridChecked;

        public int GridSize
        {
            get => Get<int>();
            set => Set(value); 
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
