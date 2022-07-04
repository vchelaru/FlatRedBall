using FlatRedBall.Glue.MVVM;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OfficialPlugins.PointEditingPlugin
{
    internal class PointEditingViewModel : ViewModel
    {
        public ObservableCollection<Vector2> Points 
        {
            get => Get<ObservableCollection<Vector2>>();
            set => Set(value);
        }

        public Vector2? SelectedPoint
        {
            get => Get<Vector2?>();
            set => Set(value);
        }

        public PointEditingViewModel() => Points = new ObservableCollection<Vector2>();
    }
}
