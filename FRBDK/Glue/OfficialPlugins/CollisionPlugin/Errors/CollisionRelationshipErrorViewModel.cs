using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPluginsCore.CollisionPlugin.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPluginsCore.CollisionPlugin.Errors
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
            else
            {
                return null;
            }
        }
    }
}
