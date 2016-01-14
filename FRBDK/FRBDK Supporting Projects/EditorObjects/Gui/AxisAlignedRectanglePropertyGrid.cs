using System;
using System.Collections.Generic;
using System.Text;


using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;

namespace EditorObjects.Gui
{
    public class AxisAlignedRectanglePropertyGrid : PropertyGrid<AxisAlignedRectangle>
    {
        #region Fields

        PositionedObjectList<AxisAlignedRectangle> mEditorAxisAlignedRectangleList;
        static bool IsFirstTimeCalled = true;

        #endregion

        #region Properties


        public PositionedObjectList<AxisAlignedRectangle> EditorAxisAlignedRectangleList
        {
            set
            {
                if (mEditorAxisAlignedRectangleList == null)
                {
                    mEditorAxisAlignedRectangleList = value;

                    SetMemberChangeEvent("Name", MakeSelectedObjectNameUnique);
                }
                else
                {
                    throw new ArgumentException("Can't set the AxisAlignedRectangle list again - it's already been set once.");
                }
            }
        }

        public override AxisAlignedRectangle SelectedObject
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
            FlatRedBall.Utilities.StringFunctions.MakeNameUnique<AxisAlignedRectangle>(SelectedObject, mEditorAxisAlignedRectangleList);
        }

        #endregion

        #region Methods

        public AxisAlignedRectanglePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();

            IncludeMember("X", "Basic");
            IncludeMember("Y", "Basic");

            IncludeMember("ScaleX", "Basic");
            IncludeMember("ScaleY", "Basic");

            IncludeMember("Color", "Basic");

            IncludeMember("Visible", "Basic");

            IncludeMember("Name", "Basic");

            IncludeMember("RelativeX", "Relative");
            IncludeMember("RelativeY", "Relative");

            ((UpDown)GetUIElementForMember("ScaleX")).MinValue = 0;
            ((UpDown)GetUIElementForMember("ScaleY")).MinValue = 0;

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
