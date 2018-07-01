using GlueView.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GlueView.EmbeddedPlugins.CameraControlsPlugin.ViewModels
{
    public class CameraViewModel : ViewModel
    {
        public bool ShowOrigin
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public bool ShowGrid
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(ShowGrid))]
        public Visibility CellSizeVisibility => ShowGrid ? Visibility.Visible : Visibility.Collapsed;


        public int CellSize
        {
            get { return Get<int>(); }
            set { Set(value); }
        }

        public object PropertyGridDisplayObject
        {
            get { return Get<object>(); }
            set { Set(value); }
        }
    }
}
