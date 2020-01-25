using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.Managers;
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
    #region Enums

    public enum CollisionType
    {
        NoPhysics,
        MoveCollision,
        BounceCollision,
        PlatformerSolidCollision,
        PlatformerCloudCollision
    }
    #endregion

    public class CollisionRelationshipViewModel :
        PropertyListContainerViewModel
    {
        #region Fields/Properties

        public const string EntireObject = "<Entire Object>";

        [SyncedProperty]
        public string FirstCollisionName
        {
            get { return Get<string>(); }
            set { SetAndPersist(value); }
        }

        [SyncedProperty]
        public string SecondCollisionName
        {
            get { return Get<string>(); }
            set { SetAndPersist(value); }
        }

        public ObservableCollection<string> FirstCollisionItemSource
        {
            get { return Get<ObservableCollection<string>>(); }
            set { Set(value); }
        }

        public ObservableCollection<string> SecondCollisionItemSource
        {
            get => Get<ObservableCollection<string>>();
            set => Set(value);
        }

        public bool FirstSubCollisionEnabled
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [SyncedProperty]
        public string FirstSubCollisionSelectedItem
        {
            get { return Get<string>(); }
            set { SetAndPersist(value); }
        }

        public ObservableCollection<string> FirstSubCollisionItemsSource
        {
            get { return Get<ObservableCollection<string>>(); }
            set { Set(value); }
        }


        public bool SecondSubCollisionEnabled
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [SyncedProperty]
        public string SecondSubCollisionSelectedItem
        {
            get { return Get<string>(); }
            set { SetAndPersist(value); }
        }

        public ObservableCollection<string> SecondSubCollisionItemsSource
        {
            get { return Get<ObservableCollection<string>>(); }
            set { Set(value); }
        }


        [SyncedProperty]
        public CollisionType CollisionType
        {
            get => (CollisionType)Get<int>();
            set => SetAndPersist((int)value);
        }

        [DependsOn(nameof(CollisionType))]
        public bool IsNoPhysicsChecked
        {
            get { return CollisionType == CollisionType.NoPhysics; }
            set
            {
                if (value)
                {
                    CollisionType = CollisionType.NoPhysics;
                }
            }
        }

        [DependsOn(nameof(CollisionType))]
        public bool IsMoveCollisionChecked
        {
            get { return CollisionType == CollisionType.MoveCollision; }
            set
            {
                if (value)
                {
                    CollisionType = CollisionType.MoveCollision;
                }
            }
        }

        [DependsOn(nameof(CollisionType))]
        public Visibility MoveCollisionVisibility
        {
            get
            {
                if(CollisionType == CollisionType.MoveCollision)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(CollisionType))]
        public bool IsBounceCollisionChecked
        {
            get { return CollisionType == CollisionType.BounceCollision; }
            set
            {
                if (value)
                {
                    CollisionType = CollisionType.BounceCollision;
                }
            }
        }

        [DependsOn(nameof(CollisionType))]
        public Visibility BounceCollisionVisibility
        {
            get
            {
                if(CollisionType == CollisionType.BounceCollision)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public bool IsFirstPlatformer
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsFirstPlatformer))]
        public Visibility PlatformerOptionsVisibility => IsFirstPlatformer ? Visibility.Visible : Visibility.Collapsed;

        [DependsOn(nameof(CollisionType))]
        public bool IsPlatformerSolidCollisionChecked
        {
            get { return CollisionType == CollisionType.PlatformerSolidCollision; }
            set
            {
                if (value)
                {
                    CollisionType = CollisionType.PlatformerSolidCollision;
                }
            }
        }

        [DependsOn(nameof(CollisionType))]
        public bool IsPlatformerCloudCollisionChecked
        {
            get { return CollisionType == CollisionType.PlatformerCloudCollision; }
            set
            {
                if (value)
                {
                    CollisionType = CollisionType.PlatformerCloudCollision;
                }
            }
        }

        [SyncedProperty]
        [DefaultValue(1.0f)]
        public float FirstCollisionMass
        {
            get { return Get<float>(); }
            set { SetAndPersist(value); }
        }

        [SyncedProperty]
        [DefaultValue(1.0f)]
        public float SecondCollisionMass
        {
            get { return Get<float>(); }
            set { SetAndPersist(value); }
        }

        [SyncedProperty]
        [DefaultValue(1.0f)]
        public float CollisionElasticity
        {
            get { return Get<float>(); }
            set { SetAndPersist(value); }
        }

        [DependsOn(nameof(WarningText))]
        public Visibility WarningVisibility
        {
            get
            {
                if(!string.IsNullOrEmpty(WarningText))
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [SyncedProperty]
        public bool IsAutoNameEnabled
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }


        [SyncedProperty]
        [DefaultValue(true)]
        public bool IsCollisionActive
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [DependsOn(nameof(IsCollisionActive))]
        public Visibility InactiveMessageVisibility => IsCollisionActive ?
            Visibility.Collapsed : Visibility.Visible;

        [DependsOn(nameof(GlueObject))]
        [DependsOn(nameof(FirstCollisionName))]
        [DependsOn(nameof(SecondCollisionName))]
        public string WarningText
        {
            get
            {
                var nos = GlueObject as NamedObjectSave;

                if(nos != null)
                {
                    bool isFirstList;
                    bool isSecondList;

                    var firstType = AssetTypeInfoManager.GetFirstGenericType(
                        nos, out isFirstList);
                    var secondType = AssetTypeInfoManager.GetSecondGenericType(
                        nos, out isSecondList);

                    var isFirstTileShapeCollection = firstType == "FlatRedBall.TileCollisions.TileShapeCollection";
                    var isSecondTileShapeCollection = secondType == "FlatRedBall.TileCollisions.TileShapeCollection";

                    var isFirstShapeCollection = firstType == "FlatRedBall.Math.Geometry.ShapeCollection";
                    var isSecondShapeCollection = secondType == "FlatRedBall.Math.Geometry.ShapeCollection";

                    var isFirstCollidable = !string.IsNullOrEmpty(FirstCollisionName) &&
                        !isFirstList &&
                        !isFirstTileShapeCollection &&
                        !isFirstShapeCollection;

                    var isSecondCollidable = !string.IsNullOrEmpty(SecondCollisionName) &&
                        !isSecondList &&
                        !isSecondTileShapeCollection &&
                        !isSecondShapeCollection;

                    if(isFirstTileShapeCollection)
                    {
                        return "First object cannot be a TileShapeCollection - " +
                            "only the second";
                    }
                    if(isFirstShapeCollection)
                    {
                        return "First object cannot be a ShapeCollection - " +
                            "only the second";
                    }

                    if (!string.IsNullOrEmpty(FirstCollisionName) &&
                        FirstCollisionName == SecondCollisionName)
                    {
                        if(isFirstCollidable && isSecondCollidable)
                        {
                            return $"Cannot create relationship for collidable " +
                                $"{FirstCollisionName} against itself";
                        }
                    }
                }
                return null;
            }
        }

        #endregion

        public CollisionRelationshipViewModel()
        {
            FirstCollisionItemSource = new ObservableCollection<string>();
            SecondCollisionItemSource = new ObservableCollection<string>();

            FirstSubCollisionItemsSource = new ObservableCollection<string>();
            SecondSubCollisionItemsSource = new ObservableCollection<string>();
        }

        public void UpdateMassesForTileShapeCollectionCollision()
        {
            this.FirstCollisionMass = 0;
            this.SecondCollisionMass = 1;
        }
    }
}
