{CompilerDirectives}

using FlatRedBall.Content.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Models
{
    #region Enums

    public enum SourceType
    {
        File,
        Entity,
        FlatRedBallType,
        CustomType
    }

    #endregion

    public class PropertySave
    {
        public string Name;

        public object Value;

        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }

    public static class PropertySaveListExtensions
    {
        public static object GetValue(this List<PropertySave> propertySaveList, string nameToSearchFor)
        {
            foreach (PropertySave propertySave in propertySaveList)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    return propertySave.Value;
                }
            }
            return null;
        }

        public static T GetValue<T>(this List<PropertySave> propertySaveList, string nameToSearchFor)
        {
            var copy = propertySaveList.ToArray();
            foreach (PropertySave propertySave in copy)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    var uncastedValue = propertySave.Value;
                    if (typeof(T) == typeof(int) && uncastedValue is long asLong)
                    {
                        return (T)((object)(int)asLong);
                    }
                    else if (typeof(T) == typeof(float) && uncastedValue is double asDouble)
                    {
                        return (T)((object)(float)asDouble);
                    }
                    else
                    {
                        return (T)propertySave.Value;
                    }
                }
            }
            return default(T);
        }
    }

    public class NamedObjectSave
    {
        public List<PropertySave> Properties
        {
            get;
            set;
        } = new List<PropertySave>();

        public SourceType SourceType
        {
            get;
            set;
        }

        public string SourceClassType
        {
            get;
            set;
        }

        string mSourceClassGenericType;

        public string SourceClassGenericType
        {
            get { return mSourceClassGenericType; }
            set
            {
                mSourceClassGenericType = value;

                if (mSourceClassGenericType == "<NONE>")
                {
                    mSourceClassGenericType = null;
                }
            }
        }

        public string InstanceName
        {
            get;
            set;
        }

        public bool AddToManagers
        {
            get; set;
        }

        public bool IncludeInIVisible
        {
            get;
            set;
        }

        public bool IncludeInICollidable
        {
            get;
            set;
        }


        public bool IncludeInIClickable
        {
            get;
            set;
        }

        public List<InstructionSave> InstructionSaves = new List<InstructionSave>();

        public NamedObjectSave()
        {
            //GenerateTimedEmit = true;
            //Instantiate = true;
            //mTypedMembersReadOnly = new ReadOnlyCollection<TypedMemberBase>(mTypedMembers);
            ////Events = new List<EventSave>();

            IncludeInIVisible = true;
            IncludeInIClickable = true;
            IncludeInICollidable = true;
            //CallActivity = true;

            //AttachToContainer = true;
            AddToManagers = true;

            //FulfillsRequirement = "<NONE>";

            //ContainedObjects = new List<NamedObjectSave>();

            //// Sept 25, 2020
            //// This used to be 
            //// true, but this causes
            //// unexpected behavior when 
            //// 2D games are resized. If we
            //// set this to false, then layers
            //// will automatically match the camera,
            //// which probably matches what the user expects
            ////IndependentOfCamera = true;
            //IndependentOfCamera = false;
        }
    }

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

    }
}
