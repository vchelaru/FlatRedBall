using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.ExtensionMethods;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.CollisionPlugin.Errors
{
    public class CollisionRelationshipErrorViewModel : ErrorViewModel
    {
        NamedObjectSave CollisionRelationship;
        IElement Container;

        public override string UniqueId => Details;

        public CollisionRelationshipErrorViewModel(NamedObjectSave collisionRelationship,
            IElement container)
        {
            this.CollisionRelationship = collisionRelationship;
            this.Container = container;

            this.Details = TryGetErrorMessageFor(collisionRelationship, container);
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentNamedObjectSave = CollisionRelationship;
            GlueCommands.Self.DialogCommands.FocusTab("Collision");
        }

        public static string TryGetErrorMessageFor(NamedObjectSave namedObject, IElement container)
        {
            var firstCollidable = namedObject.GetFirstCollidableObjectName();
            var secondCollidable = namedObject.GetSecondCollidableObjectName();

            bool isSecondNullInError()
            {
                if (string.IsNullOrWhiteSpace(secondCollidable))
                {
                    var isFirstList = container.GetNamedObject(firstCollidable)?.IsList == true;
                    // As of this writing (Apr 11, 2021) only list vs "null" can create an Always collision
                    return !isFirstList;
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(firstCollidable))
            {
                return $"CollisionRelationship {namedObject.InstanceName} has an empty first collidable in {container}";
            }
            else if (isSecondNullInError())
            {
                return $"CollisionRelationship {namedObject.InstanceName} has an empty second collidable in {container}";
            }
            else if (container.AllNamedObjects.Any(item => item.InstanceName == firstCollidable) == false)
            {
                return $"CollisionRelationship {namedObject.InstanceName} references missing object {firstCollidable} in {container}";
            }
            // we handle null second collidables above
            else if (!string.IsNullOrEmpty(secondCollidable) && container.AllNamedObjects.Any(item => item.InstanceName == secondCollidable) == false)
            {
                return $"CollisionRelationship {namedObject.InstanceName} references missing object {secondCollidable} in {container}";
            }
            else if (BothMassesAreZero(namedObject))
            {
                return $"CollisionRelationship {namedObject.InstanceName} in {container} has both masses set to 0, which will crash at runtime. Change at least one mass to a non-zero value.";
            }
            else
            {
                return null;
            }
        }

        static bool BothMassesAreZero(NamedObjectSave namedObject)
        {
            var collisionType = (CollisionType)namedObject.Properties.GetValue<int>(
                nameof(CollisionRelationshipViewModel.CollisionType));

            var usesMass = collisionType == CollisionType.MoveCollision ||
                collisionType == CollisionType.BounceCollision ||
                collisionType == CollisionType.MoveSoftCollision;

            if (!usesMass)
            {
                return false;
            }

            // Masses default to 1 in codegen if the property was never explicitly set on this
            // NamedObjectSave (see CollisionCodeGenerator), so an absent property is not a 0.
            float GetMassOrDefault(string propertyName)
            {
                var isExplicitlySet = namedObject.Properties.Any(item => item.Name == propertyName);
                return isExplicitlySet ? namedObject.Properties.GetValue<float>(propertyName) : 1f;
            }

            var firstMass = GetMassOrDefault(nameof(CollisionRelationshipViewModel.FirstCollisionMass));
            var secondMass = GetMassOrDefault(nameof(CollisionRelationshipViewModel.SecondCollisionMass));

            return firstMass == 0 && secondMass == 0;
        }
    }
}
