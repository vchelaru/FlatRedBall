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

        public int SelectedIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedIndex))]
        public bool IsMoveUpEnabled => SelectedIndex > 0;

        [DependsOn(nameof(SelectedIndex))]
        [DependsOn(nameof(Points))]
        public bool IsMoveDownEnabled => SelectedIndex < Points.Count - 1;

        public PointEditingViewModel() => Points = new ObservableCollection<Vector2>();
    }
}
