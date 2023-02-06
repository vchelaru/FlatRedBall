using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Collision;
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
        NoPhysics = 0,
        MoveCollision = 1,
        BounceCollision = 2,
        PlatformerSolidCollision = 3,
        PlatformerCloudCollision = 4,
        DelegateCollision = 5,
        StackingCollision = 6,
        MoveSoftCollision = 7,
    }

    public enum CollisionDestroyType
    {
        Always,
        OnlyIfDealtDamage
    }

    #endregion

    public class CollisionRelationshipViewModel :
        PropertyListContainerViewModel
    {
        #region Consts

        public const string EntireObject = "<Entire Object>";
        public const string AlwaysColliding = "<Always Colliding>";
        public const string SelfCollisionSuffix = "(self collision)";

        #endregion

        #region Object and Subcollision Object

        [SyncedProperty]
        public string FirstCollisionName
        {
            get => Get<string>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public string SecondCollisionName
        {
            get
            {
                var toReturn = Get<string>(); 

                if (string.IsNullOrEmpty(toReturn))
                {
                    return AlwaysColliding;
                }
                else
                {
                    return toReturn;
                }
            }
            set
            {
                if (value == AlwaysColliding)
                {
                    SetAndPersist((string)null);
                }
                else
                {
                    SetAndPersist(value);
                }
            }
        }

        [DependsOn(nameof(SecondCollisionName))]
        public bool IsFirstAlwaysColliding => string.IsNullOrEmpty(SecondCollisionName) || SecondCollisionName == AlwaysColliding;

        [DependsOn(nameof(FirstCollisionName))]
        [DependsOn(nameof(IsFirstList))]
        public string FirstMassText
        {
            get
            {
                if(IsFirstList && FirstCollisionName?.EndsWith("List") == true)
                {
                    var withoutList =
                        FirstCollisionName.Substring(0, FirstCollisionName.Length-4);
                    return $"{withoutList} Mass";
                }
                else
                {
                    return $"{FirstCollisionName} Mass";
                }
            }
        }

        [DependsOn(nameof(SecondCollisionName))]
        [DependsOn(nameof(IsSecondList))]
        public string SecondMassText
        {
            get
            {
                if(IsSecondList && SecondCollisionName?.EndsWith("List") == true)
                {
                    return $"{SecondCollisionName.Substring(0, SecondCollisionName.Length-4)} Mass";
                }
                else
                {
                    return $"{SecondCollisionName} Mass";
                }
            }
        }

        public string FirstIndividualType
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(FirstIndividualType))]
        public string FirstIndividualStrippedType
        {
            get
            {
                var firstType = FirstIndividualType;
                if (firstType?.Contains(".") == true)
                {
                    var lastDot = firstType.LastIndexOf('.');
                    firstType = firstType.Substring(lastDot + 1);
                }
                return firstType;
            }
        }

        [DependsOn(nameof(SecondIndividualType))]
        public string SecondIndividualStrippedType
        {
            get
            {
                var secondType = SecondIndividualType;
                if(secondType?.Contains(".") == true)
                {
                    var lastDot = secondType.LastIndexOf(".");
                    secondType = secondType.Substring(lastDot + 1);
                }
                return secondType;
            }
        }

        public string SecondIndividualType
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsFirstList
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSecondList
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsFirstPartitioned
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSecondPartitioned
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsFirstPartitioned))]
        public Visibility FirstPartitionEnabledVisibility => IsFirstPartitioned.ToVisibility();
        [DependsOn(nameof(IsFirstPartitioned))]
        public Visibility FirstNoPartitioningVisibility => (!IsFirstPartitioned).ToVisibility();

        [DependsOn(nameof(IsSecondPartitioned))]
        public Visibility SecondPartitionEnabledVisibility => IsSecondPartitioned.ToVisibility();
        [DependsOn(nameof(IsSecondPartitioned))]
        public Visibility SecondNoPartitioningVisibility => (!IsSecondPartitioned).ToVisibility();


        public ObservableCollection<string> FirstCollisionItemSource
        {
            get => Get<ObservableCollection<string>>(); 
            set => Set(value); 
        }

        public ObservableCollection<string> SecondCollisionItemSource
        {
            get => Get<ObservableCollection<string>>();
            set => Set(value);
        }

        public bool FirstSubCollisionEnabled
        {
            get => Get<bool>(); 
            set => Set(value); 
        }

        [SyncedProperty]
        public string FirstSubCollisionSelectedItem
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }

        public ObservableCollection<string> FirstSubCollisionItemsSource
        {
            get => Get<ObservableCollection<string>>(); 
            set => Set(value); 
        }


        public bool SecondSubCollisionEnabled
        {
            get => Get<bool>(); 
            set => Set(value); 
        }

        [SyncedProperty]
        public string SecondSubCollisionSelectedItem
        {
            get => Get<string>(); 
            set => SetAndPersist(value); 
        }

        public ObservableCollection<string> SecondSubCollisionItemsSource
        {
            get => Get<ObservableCollection<string>>(); 
            set => Set(value); 
        }

        [DependsOn(nameof(CollisionType))]
        public Visibility SubcollisionDropdownVisibility => (CollisionType != CollisionType.DelegateCollision).ToVisibility();

        [DependsOn(nameof(CollisionType))]
        public Visibility NoSubcollisionMessageVisibility => (CollisionType == CollisionType.DelegateCollision).ToVisibility();

        #endregion

        #region Warning
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

        [SyncedProperty]
        public bool IsAutoNameEnabled
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        #region Active/Inactive

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

        #endregion

        #region Collision Limit

        [DependsOn(nameof(IsFirstList))]
        [DependsOn(nameof(IsSecondList))]
        public Visibility CollisionLimitUiVisibility => (IsFirstList && IsSecondList).ToVisibility();

        [DependsOn(nameof(IsFirstList))]
        [DependsOn(nameof(IsSecondList))]
        public Visibility ListVsListDuplicateVisibility => (IsFirstList && IsSecondList).ToVisibility();

        [SyncedProperty]
        public ListVsListLoopingMode ListVsListLoopingMode
        {
            get => (ListVsListLoopingMode)Get<int>();
            set => SetAndPersist((int)value);
        }

        [DependsOn(nameof(ListVsListLoopingMode))]
        public bool IsPreventDoubleChecksChecked
        {
            get => ListVsListLoopingMode == ListVsListLoopingMode.PreventDoubleChecksPerFrame;
            set
            {
                if (value) ListVsListLoopingMode = ListVsListLoopingMode.PreventDoubleChecksPerFrame;
            }
        }

        [DependsOn(nameof(ListVsListLoopingMode))]
        public bool IsAllowDoubleChecksChecked
        {
            get => ListVsListLoopingMode == ListVsListLoopingMode.AllowDoubleChecksPerFrame;
            set
            {
                if (value) ListVsListLoopingMode = ListVsListLoopingMode.AllowDoubleChecksPerFrame;
            }
        }

        #endregion

        #region Physics

        [SyncedProperty]
        public CollisionType CollisionType
        {
            get => (CollisionType)Get<int>();
            set => SetAndPersist((int)value);
        }

        [DependsOn(nameof(CollisionType))]
        public bool IsNoPhysicsChecked
        {
            get => CollisionType == CollisionType.NoPhysics; 
            set
            {
                if (value) CollisionType = CollisionType.NoPhysics;
            }
        }

        [DependsOn(nameof(CollisionType))]
        public bool IsMoveCollisionChecked
        {
            get => CollisionType == CollisionType.MoveCollision; 
            set
            {
                if (value) CollisionType = CollisionType.MoveCollision;
            }
        }

        [DependsOn(nameof(CollisionType))]
        public Visibility MoveCollisionVisibility
        {
            get =>
                (CollisionType == CollisionType.MoveCollision).ToVisibility();
        }

        [DependsOn(nameof(CollisionType))]
        public bool IsMoveSoftCollisionChecked
        {
            get => CollisionType == CollisionType.MoveSoftCollision;
            set
            {
                if (value) CollisionType = CollisionType.MoveSoftCollision;
            }
        }


        [DependsOn(nameof(CollisionType))]
        public Visibility MoveSoftCollisionVisibility
        {
            get => 
                (CollisionType == CollisionType.MoveSoftCollision).ToVisibility();
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
            get =>
                (CollisionType == CollisionType.BounceCollision).ToVisibility();
        }

        [SyncedProperty]
        public CollisionLimit CollisionLimit
        {
            get => (CollisionLimit)Get<int>();
            set => SetAndPersist((int)value);
        }

        [DependsOn(nameof(CollisionLimit))]
        public bool IsAllCollisionLimitChecked
        {
            get => CollisionLimit == CollisionLimit.All;
            set
            {
                if(value)
                {
                    CollisionLimit = CollisionLimit.All;
                }
            }
        }

        [DependsOn(nameof(CollisionLimit))]
        public bool IsFirstCollisionLimitChecked
        {
            get => CollisionLimit == CollisionLimit.First;
            set
            {
                if(value)
                {
                    CollisionLimit = CollisionLimit.First;
                }
            }
        }

        [DependsOn(nameof(CollisionLimit))]
        public bool IsClosestCollisionLimitChecked
        {
            get => CollisionLimit == CollisionLimit.Closest;
            set
            {
                if(value)
                {
                    CollisionLimit = CollisionLimit.Closest;
                }
            }
        }

        [DependsOn(nameof(CollisionLimit))]
        [DependsOn(nameof(FirstIndividualStrippedType))]
        [DependsOn(nameof(SecondIndividualStrippedType))]
        public string CollisionLimitExplanationText
        {
            get
            {
                switch (CollisionLimit)
                {
                    case CollisionLimit.All:
                        return $"Each {FirstIndividualStrippedType} will attempt to collide against each {SecondIndividualStrippedType} each frame";
                    case CollisionLimit.First:
                        return $"Each {FirstIndividualStrippedType} will only collide at most with one {SecondIndividualStrippedType} each frame";
                    case CollisionLimit.Closest:
                        return $"Each {FirstIndividualStrippedType} will collide with the closest {SecondIndividualStrippedType} each frame";
                }
                return "";
            }
        }

        [DependsOn(nameof(IsFirstAlwaysColliding))]
        public Visibility CollisionPhysicsUiVisibility => (IsFirstAlwaysColliding == false).ToVisibility();


        public bool IsSecondStackable
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsSecondTileShapeCollection
        {
            get => Get<bool>();
            set => Set(value);
        }


        [DependsOn(nameof(IsFirstPlatformer))]
        public Visibility PlatformerOptionsVisibility => IsFirstPlatformer.ToVisibility();

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

        [DependsOn(nameof(IsFirstPlatformer))]
        [DependsOn(nameof(IsPlatformerSolidCollisionChecked))]
        public Visibility PlatformerMovementValuesVisibility =>
            (IsFirstPlatformer).ToVisibility();


        [DependsOn(nameof(CollisionType))]
        public bool IsPlatformerCloudCollisionChecked
        {
            get => CollisionType == CollisionType.PlatformerCloudCollision;
            set
            {
                if (value)
                {
                    CollisionType = CollisionType.PlatformerCloudCollision;
                }
            }
        }

        [DependsOn(nameof(CollisionType))]
        public bool IsDelegateCollisionChecked
        {
            get => CollisionType == CollisionType.DelegateCollision;
            set
            {
                if(value) CollisionType = CollisionType.DelegateCollision;
            }
        }

        [DependsOn(nameof(CollisionType))]
        public bool IsStackingCollisionChecked
        {
            get => CollisionType == CollisionType.StackingCollision;
            set
            {
                if (value) CollisionType = CollisionType.StackingCollision;
            }
        }

        public bool SupportsManualPhysics
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(SupportsManualPhysics))]
        public Visibility AutomaticallyApplyPhysicsVisibility => SupportsManualPhysics.ToVisibility();



        [SyncedProperty]
        [DefaultValue(true)]
        public bool IsAutomaticallyApplyPhysicsChecked
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [DependsOn(nameof(IsFirstStackable))]
        [DependsOn(nameof(IsSecondStackable))]
        [DependsOn(nameof(IsSecondTileShapeCollection))]
        public Visibility StackingCollisionVisibility =>
            (IsFirstStackable && 
            (IsSecondStackable || IsSecondTileShapeCollection)).ToVisibility();

        [SyncedProperty]
        [DefaultValue(1.0f)]
        public float FirstCollisionMass
        {
            get => Get<float>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        [DefaultValue(1.0f)]
        public float SecondCollisionMass
        {
            get => Get<float>(); 
            set => SetAndPersist(value); 
        }

        [SyncedProperty]
        [DefaultValue(1.0f)]
        public float SoftCollisionCoefficient
        {
            get => Get<float>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        [DefaultValue(1.0f)]
        public float CollisionElasticity
        {
            get => Get<float>(); 
            set => SetAndPersist(value); 
        }


        #endregion

        #region Platformer

        public bool IsFirstPlatformer
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsFirstStackable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public ObservableCollection<string> AvailablePlatformerVariableNames
        {
            get; private set;
        } = new ObservableCollection<string>();

        [DependsOn(nameof(FirstIndividualType))]
        public string PlatformerValuesGroupHeader
        {
            get
            {
                string strippedName = FirstIndividualType;
                if (FirstIndividualType?.Contains(".") == true)
                {
                    strippedName = FirstIndividualType.Substring(FirstIndividualType.LastIndexOf(".") + 1);
                }
                return $"{strippedName} Platformer Movement Values";
            }
        }

        [SyncedProperty]
        public string GroundPlatformerVariableName
        {
            get => Get<string>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public string AirPlatformerVariableName
        {
            get => Get<string>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public string AfterDoubleJumpPlatformerVariableName
        {
            get => Get<string>();
            set => SetAndPersist(value);
        }

        #endregion

        #region Damage

        bool UsesDamageV2 => GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.DamageableHasHealth;


        public bool IsFirstDamageable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSecondDamageable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsFirstDamageArea
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSecondDamageArea
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsFirstDamageable))]
        [DependsOn(nameof(IsSecondDamageable))]
        [DependsOn(nameof(IsFirstDamageArea))]
        [DependsOn(nameof(IsSecondDamageArea))]
        public Visibility DamageValuesVisibility =>
            // Only need to check if one is damageable. It doesn't matter
            // if the other is a dmaage area.
            //((IsFirstDamageable && IsSecondDamageArea) ||
            //(IsSecondDamageable && IsFirstDamageArea)).ToVisibility();
            (UsesDamageV2 && (IsSecondDamageArea || IsFirstDamageArea)).ToVisibility();

        [SyncedProperty]
        public bool IsDealDamageChecked
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }


        [DependsOn(nameof(IsFirstDamageArea))]
        [DependsOn(nameof(IsSecondDamageArea))]
        public Visibility DealDamageCheckBoxVisibility => (IsFirstDamageable || IsSecondDamageable).ToVisibility();

        [DependsOn(nameof(IsFirstDamageArea))]
        public Visibility DestroyFirstOnDamageVisibility => IsFirstDamageArea.ToVisibility();
        [DependsOn(nameof(IsSecondDamageArea))]
        public Visibility DestroySecondOnDamageVisibility => IsSecondDamageArea.ToVisibility();

        [SyncedProperty]
        public bool IsDestroyFirstOnDamageChecked
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [DependsOn(nameof(IsDestroyFirstOnDamageChecked))]
        [DependsOn(nameof(DestroyFirstOnDamageVisibility))]
        public Visibility FirstDestructionOptionsVisibility =>
            (IsDestroyFirstOnDamageChecked && DestroyFirstOnDamageVisibility == Visibility.Visible).ToVisibility();

        [SyncedProperty]
        public CollisionDestroyType FirstCollisionDestroyType
        {
            get => Get<CollisionDestroyType>();
            set => SetAndPersist(value);
        }

        [DependsOn(nameof(FirstCollisionDestroyType))]
        public bool IsFirstAlwaysDestroyChecked
        {
            get => FirstCollisionDestroyType== CollisionDestroyType.Always;
            set { if(value) FirstCollisionDestroyType= CollisionDestroyType.Always; }
        }
        [DependsOn(nameof(FirstCollisionDestroyType))]
        public bool IsFirstOnlyIfDamageDestroyChecked
        {
            get => FirstCollisionDestroyType== CollisionDestroyType.OnlyIfDealtDamage;
            set { if (value) FirstCollisionDestroyType = CollisionDestroyType.OnlyIfDealtDamage; }
        }



        [SyncedProperty]
        public bool IsDestroySecondOnDamageChecked
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [DependsOn(nameof(IsDestroySecondOnDamageChecked))]
        [DependsOn(nameof(DestroySecondOnDamageVisibility))]
        public Visibility SecondDestructionOptionsVisibility =>
            (IsDestroySecondOnDamageChecked && DestroySecondOnDamageVisibility == Visibility.Visible).ToVisibility();

        [SyncedProperty]
        public CollisionDestroyType SecondCollisionDestroyType
        {
            get => Get<CollisionDestroyType>();
            set => SetAndPersist(value);
        }

        [DependsOn(nameof(SecondCollisionDestroyType))]
        public bool IsSecondAlwaysDestroyChecked
        {
            get => SecondCollisionDestroyType== CollisionDestroyType.Always;
            set { if (value) SecondCollisionDestroyType = CollisionDestroyType.Always; }
        }

        [DependsOn(nameof(SecondCollisionDestroyType))]
        public bool IsSecondOnlyIfDamageDestroyChecked
        {
            get => SecondCollisionDestroyType == CollisionDestroyType.OnlyIfDealtDamage;
            set { if (value) SecondCollisionDestroyType = CollisionDestroyType.OnlyIfDealtDamage; }
        }


        [DependsOn(nameof(IsSecondDamageable))]
        string FirstDestroyDamageOrCollision => IsSecondDamageable ? "Damage" : "Collision";
        [DependsOn(nameof(FirstIndividualStrippedType))]
        [DependsOn(nameof(FirstDestroyDamageOrCollision))]
        public string DestroyFirstOnDamageText => $"Destroy {FirstIndividualStrippedType} on {FirstDestroyDamageOrCollision}";


        [DependsOn(nameof(IsFirstDamageable))]
        string SecondDestroyDamageOrCollision => IsFirstDamageable ? "Damage" : "Collision";
        [DependsOn(nameof(SecondIndividualStrippedType))]
        [DependsOn(nameof(SecondDestroyDamageOrCollision))]
        public string DestroySecondOnDamageText => $"Destroy {SecondIndividualStrippedType} on {SecondDestroyDamageOrCollision}";

        #endregion

        #region Events

        public ObservableCollection<EventResponseSave> Events
        {
            get => Get<ObservableCollection<EventResponseSave>>();
            private set => Set(value);
        }

        public Visibility AddEventButtonVisibility
        {
            get => Events.Count == 0 ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion

        public CollisionRelationshipViewModel()
        {
            FirstCollisionItemSource = new ObservableCollection<string>();
            SecondCollisionItemSource = new ObservableCollection<string>();

            FirstSubCollisionItemsSource = new ObservableCollection<string>();
            SecondSubCollisionItemsSource = new ObservableCollection<string>();

            Events = new ObservableCollection<EventResponseSave>();
            Events.CollectionChanged += (not, used) => 
                NotifyPropertyChanged(nameof(AddEventButtonVisibility));
        }

        public void UpdateMassesForTileShapeCollectionCollision()
        {
            this.FirstCollisionMass = 0;
            this.SecondCollisionMass = 1;
        }
    }
}
