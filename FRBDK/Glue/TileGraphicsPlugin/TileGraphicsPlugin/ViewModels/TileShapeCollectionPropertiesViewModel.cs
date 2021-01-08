using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TileGraphicsPlugin.ViewModels
{
    #region Enumerations

    public enum CollisionCreationOptions
    {
        Empty = 0,
        FillCompletely,
        BorderOutline,
        FromProperties,
        FromType,
        FromLayer
    }

    #endregion

    public class TileShapeCollectionPropertiesViewModel : PropertyListContainerViewModel
    {
        [SyncedProperty]
        // Vic asks - shouldn't the default value be (int)?
        [DefaultValue(CollisionCreationOptions.Empty)]
        public CollisionCreationOptions CollisionCreationOptions
        {
            get => (CollisionCreationOptions)Get<int>(); 
            set => SetAndPersist((int)value); 
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsEmptyChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.Empty;
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.Empty;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFillCompletelyChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.FillCompletely;
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FillCompletely;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FillDimensionsVisibility => CollisionCreationOptions == CollisionCreationOptions.FillCompletely ?
                  Visibility.Visible :
                  Visibility.Collapsed;

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsBorderChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.BorderOutline; 
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.BorderOutline;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility BorderOutlineVisibility
        {
            get
            {
                return CollisionCreationOptions == CollisionCreationOptions.BorderOutline 
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFromPropertiesChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.FromProperties; 
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FromProperties;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FromPropertiesVisibility => CollisionCreationOptions == CollisionCreationOptions.FromProperties ?
                      Visibility.Visible :
                      Visibility.Collapsed;

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFromTypeChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.FromType; 
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FromType;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FromTypeVisibility => CollisionCreationOptions == CollisionCreationOptions.FromType ?
                    Visibility.Visible :
                    Visibility.Collapsed;

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFromLayerChecked
        {
            get => CollisionCreationOptions == CollisionCreationOptions.FromLayer; 
            set
            {
                if(value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FromLayer;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FromLayerVisibility => CollisionCreationOptions == CollisionCreationOptions.FromLayer ?
                  Visibility.Visible :
                  Visibility.Collapsed;

        [SyncedProperty]
        public string CollisionLayerName
        {
            get => Get<string>();
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        public string CollisionLayerTileType
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        public bool IsCollisionMerged
        {
            get => Get<bool>(); 
            set => SetAndPersist(value); 
        }

        public ObservableCollection<string> TmxObjectNames
        {
            get => Get<ObservableCollection<string>>(); 
            set => Set(value);
        }

        public ObservableCollection<string> AvailableTypes
        {
            get => Get<ObservableCollection<string>>(); 
            set => Set(value); 
        } 

        [SyncedProperty]
        [DefaultValue(16.0f)]
        public float CollisionTileSize
        {
            get => Get<float>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        public float CollisionFillLeft
        {
            get => Get<float>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        public float CollisionFillTop
        {
            get => Get<float>(); 
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        [DefaultValue(32)]
        public int CollisionFillWidth
        {
            get => Get<int>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        [DefaultValue(1)]
        public int CollisionFillHeight
        {
            get => Get<int>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        public string SourceTmxName
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }

        // for now a single string, eventually a list?
        [SyncedProperty]
        public string CollisionPropertyName
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        public string CollisionTileTypeName
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        public bool RemoveTilesAfterCreatingCollision
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        public bool IsEntireViewEnabled
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public TileShapeCollectionPropertiesViewModel()
        {
            AvailableTypes = new ObservableCollection<string>();

        }
    }
}
