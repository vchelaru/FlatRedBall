using FlatRedBall.Glue.MVVM;
using FlatRedBall.Math;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.CollisionPlugin.ViewModels
{

    public class CollidableNamedObjectRelationshipViewModel : PropertyListContainerViewModel
    {
        #region Partitioning

        public bool CanBePartitioned
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(CanBePartitioned))]
        public Visibility PartitioningControlUiVisibility => CanBePartitioned.ToVisibility();

        [DependsOn(nameof(CanBePartitioned))]
        public Visibility AlreadyOrCantBePartitionedVisibility => (!CanBePartitioned).ToVisibility();

        [SyncedProperty]
        public bool PerformCollisionPartitioning
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [DependsOn(nameof(PerformCollisionPartitioning))]
        public Visibility PartitioningUiVisibility => PerformCollisionPartitioning.ToVisibility();

        [SyncedProperty]
        [DefaultValue(Axis.X)]
        public Axis SortAxis
        {
            get => (Axis)Get<int>();
            set => SetAndPersist((int)value);
        }

        public bool IsSortXAxisChecked
        {
            get => SortAxis == Axis.X;
            set
            {
                if(value)
                {
                    SortAxis = Axis.X;
                }
            }
        }

        public bool IsSortYAxisChecked
        {
            get => SortAxis == Axis.Y;
            set
            {
                if(value)
                {
                    SortAxis = Axis.Y;
                }
            }
        }

        [SyncedProperty]
        [DefaultValue(32f)]
        public float PartitionWidthHeight
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        [DefaultValue(true)]
        public bool IsSortListEveryFrameChecked
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        #endregion

        #region Collision Relationships 

        public string CollisionRelationshipsTitle
        {
            get => Get<string>();
            set => Set(value); 
        }


        public ObservableCollection<NamedObjectPairRelationshipViewModel> NamedObjectPairs
        {
            get => Get<ObservableCollection<NamedObjectPairRelationshipViewModel>>(); 
            set => Set(value); 
        }

        #endregion

        public CollidableNamedObjectRelationshipViewModel()
        {
            NamedObjectPairs = new ObservableCollection<NamedObjectPairRelationshipViewModel>();
}
    }
}
