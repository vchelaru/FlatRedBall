using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TileGraphicsPlugin.ViewModels
{
    public enum CollisionInclusion
    {
        EntireLayer,
        ByType
    }

    public class TileShapeCollectionPropertiesViewModel : ViewModel
    {
        public bool IsCollisionVisible
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public CollisionInclusion CollisionInclusion
        {
            get { return Get<CollisionInclusion>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(CollisionInclusion))]
        public bool IncludeEntireLayer
        {
            get { return CollisionInclusion == CollisionInclusion.EntireLayer; }
            set { if (value) CollisionInclusion = CollisionInclusion.EntireLayer; }
        }

        [DependsOn(nameof(CollisionInclusion))]
        public bool IncludeByType
        {
            get { return CollisionInclusion == CollisionInclusion.ByType; }
            set { if (value) CollisionInclusion = CollisionInclusion.ByType; }
        }

        [DependsOn(nameof(IncludeByType))]
        public Visibility TypeTextBoxVisibility
        {
            get { return IncludeByType ? Visibility.Visible : Visibility.Hidden; }
        }

        public string CollisionTileType
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
    }
}
