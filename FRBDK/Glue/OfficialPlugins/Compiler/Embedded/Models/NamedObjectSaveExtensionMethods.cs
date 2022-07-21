using GlueControl.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Models
{
    public static class NamedObjectSaveExtensionMethods
    {
        public static bool IsCollisionRelationship(this NamedObjectSave namedObjectSave)
        {

            return
                namedObjectSave.SourceClassType == "CollisionRelationship" ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollisionRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsPositionedObjectRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsListRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsPositionedObjectRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.AlwaysCollidingListCollisionRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsShapeCollection") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsListRelationship") == true ||

                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableListVsTileShapeCollectionRelationship") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableVsTileShapeCollectionRelationship") == true ||

                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateCollisionRelationship<") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateCollisionRelationshipBase<") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateSingleVsListRelationship<") == true ||
                namedObjectSave.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.DelegateListVsListRelationship<") == true ||

                namedObjectSave.SourceClassType?.StartsWith("CollisionRelationship<") == true;
        }


        public static NamedObjectSave GetNamedObject(this INamedObjectContainer namedObjectContainer, string namedObjectName)
        {
            return GetNamedObjectInList(namedObjectContainer.NamedObjects, namedObjectName);
        }

        public static NamedObjectSave GetNamedObjectInList(List<NamedObjectSave> namedObjectList, string namedObjectName)
        {
            for (int i = 0; i < namedObjectList.Count; i++)
            {
                NamedObjectSave nos = namedObjectList[i];

                if (nos.InstanceName == namedObjectName)
                {
                    return nos;
                }

                if (nos.ContainedObjects != null && nos.ContainedObjects.Count != 0)
                {
                    NamedObjectSave foundNos = GetNamedObjectInList(nos.ContainedObjects, namedObjectName);

                    if (foundNos != null)
                    {
                        return foundNos;
                    }
                }
            }

            return null;
        }

        public static NamedObjectSave GetNamedObjectRecursively(this INamedObjectContainer namedObjectContainer, string namedObjectName)
        {
            List<NamedObjectSave> namedObjectList = namedObjectContainer.NamedObjects;

            NamedObjectSave foundNos = GetNamedObjectInList(namedObjectList, namedObjectName);

            if (foundNos != null)
            {
                return foundNos;
            }

            // These methods need to check if the baseScreen/baseEntity is not null.
            // They can be null if the user deletes a base Screen/Entity and the tool
            // managing the Glux doesn't handle the changes.

            if (!string.IsNullOrEmpty(namedObjectContainer.BaseObject))
            {
                if (namedObjectContainer is EntitySave)
                {
                    EntitySave baseEntity = ObjectFinder.Self.GetEntitySave(namedObjectContainer.BaseObject);
                    if (baseEntity != null)
                    {
                        return GetNamedObjectRecursively(baseEntity, namedObjectName);
                    }
                }

                else if (namedObjectContainer is ScreenSave)
                {
                    ScreenSave baseScreen = ObjectFinder.Self.GetScreenSave(namedObjectContainer.BaseObject);

                    if (baseScreen != null)
                    {
                        return GetNamedObjectRecursively(baseScreen, namedObjectName);
                    }
                }
            }

            return null;
        }

    }
}
