using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace TiledPlugin.ViewModels
{
    #region Enums

    public enum TileNodeNetworkCreationOptions
    {
        Empty,
        FillCompletely,
        FromProperties,
        FromType,
        FromLayer
    }

    public enum TileNodeNetworkFromLayerOptions
    {
        AllEmpty,
        FromType
    }



    #endregion

    public class TileNodeNetworkPropertiesViewModel : PropertyListContainerViewModel
    {

        [SyncedProperty]
        public FlatRedBall.AI.Pathfinding.DirectionalType DirectionalType
        {
            get => (FlatRedBall.AI.Pathfinding.DirectionalType)Get<int>();
            set => SetAndPersist((int)value);
        }

        [DependsOn(nameof(DirectionalType))]
        public bool IsFourDirectionalTypeChecked
        {
            get => DirectionalType == FlatRedBall.AI.Pathfinding.DirectionalType.Four;
            set
            {
                if(value)
                {
                    DirectionalType = FlatRedBall.AI.Pathfinding.DirectionalType.Four;
                }
            }
        }

        [DependsOn(nameof(DirectionalType))]
        public bool IsEightDirectionalTypeChecked
        {
            get => DirectionalType == FlatRedBall.AI.Pathfinding.DirectionalType.Eight;
            set
            {
                if (value)
                {
                    DirectionalType = FlatRedBall.AI.Pathfinding.DirectionalType.Eight;
                }
            }
        }

        [SyncedProperty]
        public bool EliminateCutCorners
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        [DefaultValue((int)TileNodeNetworkCreationOptions.Empty)]
        public TileNodeNetworkCreationOptions NetworkCreationOptions
        {
            get => (TileNodeNetworkCreationOptions)Get<int>();
            set => SetAndPersist((int)value);
        }



        [DependsOn(nameof(NetworkCreationOptions))]
        public bool IsEmptyChecked
        {
            get => NetworkCreationOptions == TileNodeNetworkCreationOptions.Empty;
            set
            {
                if (value)
                {
                    NetworkCreationOptions = TileNodeNetworkCreationOptions.Empty;
                }
            }
        }

        [DependsOn(nameof(NetworkCreationOptions))]
        public bool IsFillCompletelyChecked
        {
            get => NetworkCreationOptions == TileNodeNetworkCreationOptions.FillCompletely;
            set
            {
                if (value)
                {
                    NetworkCreationOptions = TileNodeNetworkCreationOptions.FillCompletely;
                }
            }
        }

        [DependsOn(nameof(NetworkCreationOptions))]
        public Visibility FillDimensionsVisibility => NetworkCreationOptions == TileNodeNetworkCreationOptions.FillCompletely ?
                  Visibility.Visible :
                  Visibility.Collapsed;

        //[DependsOn(nameof(CollisionCreationOptions))]
        //public bool IsBorderChecked
        //{
        //    get => CollisionCreationOptions == CollisionCreationOptions.BorderOutline;
        //    set
        //    {
        //        if (value)
        //        {
        //            CollisionCreationOptions = CollisionCreationOptions.BorderOutline;
        //        }
        //    }
        //}

        //[DependsOn(nameof(CollisionCreationOptions))]
        //public Visibility BorderOutlineVisibility
        //{
        //    get
        //    {
        //        return CollisionCreationOptions == TileNodeNetworkCreationOptions.BorderOutline
        //            ? Visibility.Visible
        //            : Visibility.Collapsed;
        //    }
        //}

        [DependsOn(nameof(NetworkCreationOptions))]
        public bool IsFromPropertiesChecked
        {
            get => NetworkCreationOptions == TileNodeNetworkCreationOptions.FromProperties;
            set
            {
                if (value)
                {
                    NetworkCreationOptions = TileNodeNetworkCreationOptions.FromProperties;
                }
            }
        }

        [DependsOn(nameof(NetworkCreationOptions))]
        public Visibility FromPropertiesVisibility => NetworkCreationOptions == TileNodeNetworkCreationOptions.FromProperties ?
                      Visibility.Visible :
                      Visibility.Collapsed;

        [DependsOn(nameof(NetworkCreationOptions))]
        public bool IsFromTypeChecked
        {
            get => NetworkCreationOptions == TileNodeNetworkCreationOptions.FromType;
            set
            {
                if (value)
                {
                    NetworkCreationOptions = TileNodeNetworkCreationOptions.FromType;
                }
            }
        }

        [DependsOn(nameof(NetworkCreationOptions))]
        public Visibility FromTypeVisibility => NetworkCreationOptions == TileNodeNetworkCreationOptions.FromType ?
                    Visibility.Visible :
                    Visibility.Collapsed;

        [DependsOn(nameof(NetworkCreationOptions))]
        public bool IsFromLayerChecked
        {
            get => NetworkCreationOptions == TileNodeNetworkCreationOptions.FromLayer;
            set
            {
                if (value)
                {
                    NetworkCreationOptions = TileNodeNetworkCreationOptions.FromLayer;
                }
            }
        }

        [DependsOn(nameof(NetworkCreationOptions))]
        public Visibility FromLayerVisibility => NetworkCreationOptions == TileNodeNetworkCreationOptions.FromLayer ?
                  Visibility.Visible :
                  Visibility.Collapsed;

        [SyncedProperty]
        public string NodeNetworkLayerName
        {
            get => Get<string>();
            set => SetAndPersist(value);
        }


        [SyncedProperty]
        public string NodeNetworkLayerTileType
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

        public ObservableCollection<string> AvailableLayerNames
        {
            get => Get<ObservableCollection<string>>();
            set => Set(value);
        }

        [SyncedProperty]
        [DefaultValue(16.0f)]
        public float NodeNetworkTileSize
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public float NodeNetworkFillLeft
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public float NodeNetworkFillTop
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        [DefaultValue(32)]
        public int NodeNetworkFillWidth
        {
            get => Get<int>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        [DefaultValue(32)]
        public int NodeNetworkFillHeight
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
        public string NodeNetworkPropertyName
        {
            get => Get<string>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public string NodeNetworkTileTypeName
        {
            get => Get<string>();
            set => SetAndPersist(value);
        }

        public bool IsEntireViewEnabled
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [SyncedProperty]
        public TileNodeNetworkFromLayerOptions TileNodeNetworkFromLayerOptions
        {
            get => (TileNodeNetworkFromLayerOptions)Get<int>();
            set => SetAndPersist((int)value);
        }

        [DependsOn(nameof(TileNodeNetworkFromLayerOptions))]
        public bool IsFromLayerAllEmptySelected
        {
            get => TileNodeNetworkFromLayerOptions == TileNodeNetworkFromLayerOptions.AllEmpty;
            set
            {
                if(value)
                {
                    TileNodeNetworkFromLayerOptions = TileNodeNetworkFromLayerOptions.AllEmpty;
                }
            }
        }

        [DependsOn(nameof(TileNodeNetworkFromLayerOptions))]
        public bool IsFromLayerFromTypeSelected
        {
            get => TileNodeNetworkFromLayerOptions == TileNodeNetworkFromLayerOptions.FromType;
            set
            {
                if (value)
                {
                    TileNodeNetworkFromLayerOptions = TileNodeNetworkFromLayerOptions.FromType;
                }
            }
        }

        [DependsOn(nameof(TileNodeNetworkFromLayerOptions))]
        public Visibility FromLayerFromTypeVisibility => TileNodeNetworkFromLayerOptions == TileNodeNetworkFromLayerOptions.FromType
            ? Visibility.Visible
            : Visibility.Collapsed;

        public TileNodeNetworkPropertiesViewModel()
        {
            AvailableTypes = new ObservableCollection<string>();
            AvailableLayerNames = new ObservableCollection<string>();
        }
    }
}
