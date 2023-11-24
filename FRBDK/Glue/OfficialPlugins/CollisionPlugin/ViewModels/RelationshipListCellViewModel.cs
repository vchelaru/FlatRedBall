using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.CollisionPlugin.ViewModels
{
    public class RelationshipListCellViewModel : ViewModel
    {
        public NamedObjectSave CollisionRelationshipNamedObject
        {
            get => Get<NamedObjectSave>();
            set => Set(value);
        }

        public NamedObjectSave OwnerNamedObject
        {
            get => Get<NamedObjectSave>();
            set => Set(value);
        }

        public NamedObjectSave OtherNamedObject
        {
            get => Get<NamedObjectSave>();
            set => Set(value);
        }

        [DependsOn(nameof(CollisionRelationshipNamedObject))]
        public Visibility AddRelationshipVisibility
        {
            get
            {
                if (CollisionRelationshipNamedObject == null)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(CollisionRelationshipNamedObject))]
        public Visibility RelationshipDetailsVisibility
        {
            get
            {
                if (CollisionRelationshipNamedObject == null)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        [DependsOn(nameof(CollisionRelationshipNamedObject))]
        public string RelationshipDetails
        {
            get
            {
                if (CollisionRelationshipNamedObject != null)
                {
                    return CollisionRelationshipNamedObject.InstanceName;
                }
                else
                {
                    return null;
                }
            }
        }

        [DependsOn(nameof(CollisionRelationshipNamedObject))]
        public string RelationshipPhysicsDetails
        {
            get
            {
                if (CollisionRelationshipNamedObject != null)
                {
                    int? collision = null;
                    
                    var collisionTypeAsObject = CollisionRelationshipNamedObject.Properties.GetValue(
                        nameof(CollisionRelationshipViewModel.CollisionType));

                    if(collisionTypeAsObject is int asInt)
                    {
                        collision = asInt;
                    }
                    else if(collisionTypeAsObject is long asLong)
                    {
                        // There's a bug in FRB where the Type would not get saved properly on a collision property. Let's tolerate that here
                        // for now. This bug has been fixed in FRB in November 2023, but we'll still add this here for Cranky Chibi Cthulhu
                        collision = (int)asLong;
                    }

                    string physicsText = "No Physics";

                    if (collision != null)
                    {
                        switch ((CollisionType)collision.Value)
                        {
                            case CollisionType.NoPhysics:
                                return "No physics";
                            case CollisionType.MoveCollision:
                                return "Move collision";
                            case CollisionType.MoveSoftCollision:
                                return "Move soft collision";
                            case CollisionType.BounceCollision:
                                return $"Bounce collision{GetIfSolidSuffix()}";
                            case CollisionType.DelegateCollision:
                                return "Delegate (custom) collision";
                            case CollisionType.PlatformerSolidCollision:
                                return "Platformer solid collision";
                            case CollisionType.PlatformerCloudCollision:
                                return "Platformer cloud collision";
                            case CollisionType.StackingCollision:
                                return "Stacking collision";
                            default:
                                return collision.Value.ToString();

                        }
                    }

                    return physicsText;
                }
                else
                {
                    return string.Empty;
                }

                string GetIfSolidSuffix()
                {
                    var firstMass = CollisionRelationshipNamedObject.Properties.GetValue<float?>(
                                               nameof(CollisionRelationshipViewModel.FirstCollisionMass));
                    var secondMass = CollisionRelationshipNamedObject.Properties.GetValue<float?>(
                                                nameof(CollisionRelationshipViewModel.SecondCollisionMass));
                    if (firstMass == 0 && secondMass > 0)
                    {
                        return " (solid)";
                    }
                    else if (firstMass > 0 && secondMass == 0)
                    {
                        return " (solid)";
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }

        }

    }
}
