using FlatRedBall.Arrow.Managers;
using FlatRedBall.Glue;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace FlatRedBall.Arrow.Data
{
    internal class TypeMemberDisplayPropertiesManager : Singleton<TypeMemberDisplayPropertiesManager>
    {
        public TypeMemberDisplayProperties GetCircleDisplayProperties()
        {
            TypeMemberDisplayProperties properties = new TypeMemberDisplayProperties();
            properties.Type = typeof(FlatRedBall.Math.Geometry.Circle).FullName;
            AdjustPositionedObjectProperties(properties);

            properties.AddIgnore("LastCollisionTangent");
            properties.AddIgnore("AbsoluteVisible");
            RemoveRotationValues(properties);
            properties.SetCategory("IgnoresParentVisibility", "Attachment");

            return properties;
        }

        private static void RemoveRotationValues(TypeMemberDisplayProperties properties)
        {
            properties.AddIgnore("RotationX");
            properties.AddIgnore("RotationY");
            properties.AddIgnore("RotationZ");
            properties.AddIgnore("RelativeRotationX");
            properties.AddIgnore("RelativeRotationY");
            properties.AddIgnore("RelativeRotationZ");
        }

        public TypeMemberDisplayProperties GetAARectDisplayProperties()
        {
            TypeMemberDisplayProperties properties = new TypeMemberDisplayProperties();
            properties.Type = typeof(FlatRedBall.Math.Geometry.AxisAlignedRectangle).FullName;
            AdjustPositionedObjectProperties(properties);

            properties.AddIgnore("LastCollisionTangent");
            properties.AddIgnore("AbsoluteVisible");
            RemoveRotationValues(properties);
            properties.AddIgnore("BoundingRadius");
            properties.SetCategory("IgnoresParentVisibility", "Attachment");


            properties.SetCategory("Left", "Position");
            properties.SetCategory("Right", "Position");
            properties.SetCategory("Top", "Position");
            properties.SetCategory("Bottom", "Position");

            properties.SetCategory("RelativeLeft", "Position");
            properties.SetCategory("RelativeRight", "Position");
            properties.SetCategory("RelativeTop", "Position");
            properties.SetCategory("RelativeBottom", "Position");

            properties.AddIgnore("Red");
            properties.AddIgnore("Green");
            properties.AddIgnore("Blue");


            return properties;
        }

        public TypeMemberDisplayProperties GetGlueElementRuntimeProperties()
        {
            TypeMemberDisplayProperties properties = new TypeMemberDisplayProperties();
            properties.Type = typeof(ElementRuntime).FullName;
            AdjustPositionedObjectProperties(properties);

            properties.AddIgnore("LastCollisionTangent");
            properties.AddIgnore("AbsoluteVisible");

            properties.AddIgnore("Layer");
            properties.AddIgnore("ReferencedFileRuntimeList");
            properties.AddIgnore("CustomVariables");
            properties.AddIgnore("AssociatedIElement");
            properties.AddIgnore("AssociatedNamedObjectSave");
            properties.AddIgnore("ContainedElements");
            properties.AddIgnore("ElementsInList");
            properties.AddIgnore("EntireScenes");
            properties.AddIgnore("EntireShapeCollections");
            properties.AddIgnore("EntireEmitterLists");
            properties.AddIgnore("EntireNodeNetworks");
            properties.AddIgnore("DirectObjectReference");
            properties.AddIgnore("FieldName");
            properties.AddIgnore("ContainerName");





            //RemoveRotationValues(properties);
            properties.SetCategory("IgnoresParentVisibility", "Attachment");

            return properties;
        }

        private static void AdjustPositionedObjectProperties(TypeMemberDisplayProperties properties)
        {
            properties.AddIgnore("Children");
            properties.AddIgnore("HierarchyDepth");
            properties.AddIgnore("TopParent");

            properties.AddIgnore("KeepTrackOfReal");
            properties.AddIgnore("Drag");
            properties.AddIgnore("Instructions");

            properties.SetCategory("X", "Position");
            properties.SetCategory("Y", "Position");
            properties.SetCategory("Z", "Position");

            properties.SetCategory("RelativeX", "Position");
            properties.SetCategory("RelativeY", "Position");
            properties.SetCategory("RelativeZ", "Position");

            properties.SetCategory("RotationX", "Rotation");
            properties.SetCategory("RotationY", "Rotation");
            properties.SetCategory("RotationZ", "Rotation");

            properties.SetCategory("RelativeRotationX", "Rotation");
            properties.SetCategory("RelativeRotationY", "Rotation");
            properties.SetCategory("RelativeRotationZ", "Rotation");



            properties.AddIgnore("ChildrenAsIAttachables");
            properties.AddIgnore("ListsBelongingTo");
            properties.AddIgnore("Parent");
            properties.AddIgnore("ParentAsIAttachable");
            properties.AddIgnore("ParentBone");

            properties.SetCategory("IgnoreParentPosition", "Attachment");
            properties.SetCategory("ParentRotationChangesPosition", "Attachment");
            properties.SetCategory("ParentRotationChangesRotation", "Attachment");

            properties.SetPreferredDisplayer("RotationX", typeof(WpfDataUi.Controls.AngleSelectorDisplay));
            properties.SetPreferredDisplayer("RotationY", typeof(WpfDataUi.Controls.AngleSelectorDisplay));
            properties.SetPreferredDisplayer("RotationZ", typeof(WpfDataUi.Controls.AngleSelectorDisplay));

            properties.SetPreferredDisplayer("RelativeRotationX", typeof(WpfDataUi.Controls.AngleSelectorDisplay));
            properties.SetPreferredDisplayer("RelativeRotationY", typeof(WpfDataUi.Controls.AngleSelectorDisplay));
            properties.SetPreferredDisplayer("RelativeRotationZ", typeof(WpfDataUi.Controls.AngleSelectorDisplay));

            properties.AddIgnore("LastDependencyUpdate");
        }

        public void EliminateAbsoluteOrRelativeValuesIfNecessary(DataUiGrid dataGrid, TypeMemberDisplayProperties tmdp)
        {
            PositionedObject asPositionedObject = dataGrid.Instance as PositionedObject;

            if (asPositionedObject != null)
            {
                bool hasParent = asPositionedObject.Parent != null;

                bool isAaRect = asPositionedObject is AxisAlignedRectangle;

                TypeMemberDisplayProperties clone = tmdp.Clone();

                if (hasParent)
                {
                    SetPropertiesForHasParent(isAaRect, clone);
                }
                else
                {
                    SetPropertiesForNoParent(isAaRect, clone);
                }

                dataGrid.Apply(clone);
            }

        }

        private static void SetPropertiesForNoParent(bool isAaRect, TypeMemberDisplayProperties tmdp)
        {
            tmdp.AddIgnore("RelativeX");
            tmdp.AddIgnore("RelativeY");
            tmdp.AddIgnore("RelativeZ");

            tmdp.AddIgnore("RelativeRotationX");
            tmdp.AddIgnore("RelativeRotationY");
            tmdp.AddIgnore("RelativeRotationZ");

            if (isAaRect)
            {
                tmdp.AddIgnore("Left");
                tmdp.AddIgnore("Right");
                tmdp.AddIgnore("Top");
                tmdp.AddIgnore("Bottom");

                tmdp.AddIgnore("RelativeLeft");
                tmdp.AddIgnore("RelativeRight");
                tmdp.AddIgnore("RelativeTop");
                tmdp.AddIgnore("RelativeBottom");
            }
        }

        private static void SetPropertiesForHasParent(bool isAaRect, TypeMemberDisplayProperties tmdp)
        {
            tmdp.AddIgnore("X");
            tmdp.AddIgnore("Y");
            tmdp.AddIgnore("Z");

            tmdp.AddIgnore("RotationX");
            tmdp.AddIgnore("RotationY");
            tmdp.AddIgnore("RotationZ");

            tmdp.SetDisplay("RelativeX", "X");
            tmdp.SetDisplay("RelativeY", "Y");
            tmdp.SetDisplay("RelativeZ", "Z");

            tmdp.SetDisplay("RelativeRotationX", "RotationX");
            tmdp.SetDisplay("RelativeRotationY", "RotationY");
            tmdp.SetDisplay("RelativeRotationZ", "RotationZ");


            if (isAaRect)
            {
                tmdp.AddIgnore("Left");
                tmdp.AddIgnore("Right");
                tmdp.AddIgnore("Top");
                tmdp.AddIgnore("Bottom");

                tmdp.SetDisplay("RelativeLeft", "Left");
                tmdp.SetDisplay("RelativeRight", "Right");
                tmdp.SetDisplay("RelativeTop", "Top");
                tmdp.SetDisplay("RelativeBottom", "Bottom");
            }
        }




    }
}
