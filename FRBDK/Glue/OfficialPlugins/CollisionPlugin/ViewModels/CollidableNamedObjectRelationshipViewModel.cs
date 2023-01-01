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
    public enum PartitioningAutomaticManual
    {
        Manual,
        Automatic
    }

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

        [SyncedProperty(SyncingConditionProperty = nameof(CanBePartitioned))]
        public bool PerformCollisionPartitioning
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [DependsOn(nameof(PerformCollisionPartitioning))]
        public Visibility PartitioningUiVisibility => PerformCollisionPartitioning.ToVisibility();

        #endregion

        #region Partitioning Sort

        [SyncedProperty(SyncingConditionProperty = nameof(CanBePartitioned))]
        [DefaultValue((int)Axis.X)]
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

        #endregion

        #region Partitioning Width/Height

        [SyncedPropertyAttribute]
        [DefaultValue(PartitioningAutomaticManual.Manual)]
        public PartitioningAutomaticManual PartitioningAutomaticManual 
        { 
            get => Get<PartitioningAutomaticManual>();
            set => SetAndPersist(value); 
        }

        [DependsOn(nameof(PartitioningAutomaticManual))]
        public bool IsAutomaticPartitionSizeChecked
        {
            get => PartitioningAutomaticManual == PartitioningAutomaticManual.Automatic;
            set
            {
                if(value)
                {
                    PartitioningAutomaticManual = PartitioningAutomaticManual.Automatic;
                }
            }
        }


        [DependsOn(nameof(PartitioningAutomaticManual))]
        [DependsOn(nameof(CalculatedParitioningWidthHeight))]
        public string AutomaticRadioButtonText
        {
            get => $"Automatic ({CalculatedParitioningWidthHeight:0.0})";  
        }

        [DependsOn(nameof(IsAutomaticPartitionSizeChecked))]
        public Visibility AutomaticInfoVisibility => IsAutomaticPartitionSizeChecked.ToVisibility();

        public float CalculatedParitioningWidthHeight
        {
            get => Get<float>();
            set => Set(value);
        }

        [DependsOn(nameof(PartitioningAutomaticManual))]
        public bool IsManualPartitionSizeChecked
        {
            get => PartitioningAutomaticManual == PartitioningAutomaticManual.Manual;
            set
            {
                if (value)
                {
                    PartitioningAutomaticManual = PartitioningAutomaticManual.Manual;
                }
            }
        }

        [DependsOn(nameof(PartitioningAutomaticManual))]
        public bool IsManualTextBoxEnabled => IsManualPartitionSizeChecked;

        [SyncedProperty(SyncingConditionProperty = nameof(CanBePartitioned))]
        [DefaultValue(32f)]
        public float PartitionWidthHeight
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty(SyncingConditionProperty = nameof(CanBePartitioned))]
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
