using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;

namespace EditorObjects.Gui
{
    public class AxisAlignedCubePropertyGrid : PropertyGrid<AxisAlignedCube>
    {
        #region Fields

        PositionedObjectList<AxisAlignedCube> mEditorAxisAlignedCubeList;
        static bool IsFirstTimeCalled = true;
        #endregion

        #region Properties
       
        public PositionedObjectList<AxisAlignedCube> EditorAxisAlignedCubeList
        {
            set
            {
                if (mEditorAxisAlignedCubeList == null)
                {
                    mEditorAxisAlignedCubeList = value;

                    SetMemberChangeEvent("Name", MakeSelectedObjectNameUnique);
                }
                else
                {
                    throw new ArgumentException("Can't set the AxisAlignedCube list again - it's already been set once.");
                }
            }
        }

        public override AxisAlignedCube SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;

                Visible = (SelectedObject != null);
            }
        }

        #endregion

        #region Event Methods

        private void MakeSelectedObjectNameUnique(Window callingWindow)
        {
            // This gets called if the PropertyGrid is
            // referencing a valid list and
            // the selected object has its name changed.  When
            // this happens make sure the selected Object's name
            // is unique.
            FlatRedBall.Utilities.StringFunctions.MakeNameUnique<AxisAlignedCube>(SelectedObject, mEditorAxisAlignedCubeList);
        }

        #endregion
        
        #region Methods

        public AxisAlignedCubePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();

            IncludeMember("X", "Basic");
            IncludeMember("Y", "Basic");
            IncludeMember("Z", "Basic");

            IncludeMember("ScaleX", "Basic");
            IncludeMember("ScaleY", "Basic");
            IncludeMember("ScaleZ", "Basic");

            IncludeMember("Color", "Basic");

            IncludeMember("Visible", "Basic");

            IncludeMember("Name", "Basic");

            IncludeMember("RelativeX", "Relative");
            IncludeMember("RelativeY", "Relative");
            IncludeMember("RelativeZ", "Relative");

            ((UpDown)GetUIElementForMember("ScaleX")).MinValue = 0;
            ((UpDown)GetUIElementForMember("ScaleY")).MinValue = 0;
            ((UpDown)GetUIElementForMember("ScaleZ")).MinValue = 0;

            RemoveCategory("Uncategorized");

            SelectCategory("Basic");

            if (IsFirstTimeCalled == true)
            {
                LastXPosition = 30;
                IsFirstTimeCalled = false;
            }

			X = LastXPosition;
			Y = LastYPosition;
        }

        #endregion
    }
}


