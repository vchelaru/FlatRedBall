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
    #region Fields/Properties

    public enum CollisionCreationOptions
    {
        Empty,
        FillCompletely,
        BorderOutline,
        FromProperties,
        FromType,
        FromLayer
    }

    #endregion

    public class TileShapeCollectionPropertiesViewModel : PropertyListContainerViewModel
    {
        //[SyncedProperty]
        //public bool IsCollisionVisible
        //{
        //    get { return Get<bool>(); }
        //    set { SetAndPersist(value); }
        //}

        [SyncedProperty]
        [DefaultValue(CollisionCreationOptions.Empty)]
        public CollisionCreationOptions CollisionCreationOptions
        {
            get { return Get<CollisionCreationOptions>(); }
            set { SetAndPersist(value); }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsEmptyChecked
        {
            get { return CollisionCreationOptions == CollisionCreationOptions.Empty; }
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
            get { return CollisionCreationOptions == CollisionCreationOptions.FillCompletely; }
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FillCompletely;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FillDimensionsVisibility
        {
            get
            {
                return CollisionCreationOptions == CollisionCreationOptions.FillCompletely ?
                  Visibility.Visible :
                  Visibility.Collapsed;
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsBorderChecked
        {
            get { return CollisionCreationOptions == CollisionCreationOptions.BorderOutline; }
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
                if (CollisionCreationOptions == CollisionCreationOptions.BorderOutline)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFromPropertiesChecked
        {
            get { return CollisionCreationOptions == CollisionCreationOptions.FromProperties; }
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FromProperties;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FromPropertiesVisibility
        {
            get
            {
                return CollisionCreationOptions == CollisionCreationOptions.FromProperties ?
                      Visibility.Visible :
                      Visibility.Collapsed;
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFromTypeChecked
        {
            get { return CollisionCreationOptions == CollisionCreationOptions.FromType; }
            set
            {
                if (value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FromType;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FromTypeVisibility
        {
            get
            {
                return CollisionCreationOptions == CollisionCreationOptions.FromType ?
                    Visibility.Visible :
                    Visibility.Collapsed;
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public bool IsFromLayerChecked
        {
            get { return CollisionCreationOptions == CollisionCreationOptions.FromLayer; }
            set
            {
                if(value)
                {
                    CollisionCreationOptions = CollisionCreationOptions.FromLayer;
                }
            }
        }

        [DependsOn(nameof(CollisionCreationOptions))]
        public Visibility FromLayerVisibility
        {
            get
            {
                return CollisionCreationOptions == CollisionCreationOptions.FromLayer ?
                  Visibility.Visible :
                  Visibility.Collapsed;
            }
        }


        public ObservableCollection<string> TmxObjectNames
        {
            get { return Get<ObservableCollection<string>>(); }
            set { Set(value);}
        }

        [SyncedProperty]
        [DefaultValue(16.0f)]
        public float CollisionTileSize
        {
            get { return Get<float>(); }
            set { SetAndPersist(value); }
        }

        [SyncedProperty]
        public float CollisionFillLeft
        {
            get { return Get<float>(); }
            set { SetAndPersist(value); }
        }

        [SyncedProperty]
        public float CollisionFillTop
        {
            get { return Get<float>(); }
            set { SetAndPersist(value); }
        }

        [SyncedProperty]
        [DefaultValue(32)]
        public int CollisionFillWidth
        {
            get { return Get<int>(); }
            set { SetAndPersist(value); }
        }

        [SyncedProperty]
        [DefaultValue(1)]
        public int CollisionFillHeight
        {
            get { return Get<int>(); }
            set { SetAndPersist(value); }
        }

        [SyncedProperty]
        public string SourceTmxName
        {
            get { return Get<string>(); }
            set { SetAndPersist(value); }
        }

        // for now a single string, eventually a list?
        [SyncedProperty]
        public string CollisionPropertyName
        {
            get { return Get<string>(); }
            set { SetAndPersist(value); }
        }

        [SyncedProperty]
        public string CollisionTileTypeName
        {
            get { return Get<string>(); }
            set { SetAndPersist(value); }
        }

        public bool IsEntireViewEnabled
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
    }
}
