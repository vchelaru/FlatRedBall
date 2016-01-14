using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;

namespace EditorObjects.Gui
{
    public class CirclePropertyGrid : PropertyGrid<Circle>
    {
        #region Fields

        PositionedObjectList<Circle> mEditorCircleList;
        static bool IsFirstTimeCalled = true;
        #endregion

        #region Properties

        public PositionedObjectList<Circle> EditorCircleList
        {
            set
            {
                if (mEditorCircleList == null)
                {
                    mEditorCircleList = value;

                    SetMemberChangeEvent("Name", MakeSelectedObjectNameUnique);
                }
                else
                {
                    throw new ArgumentException("Can't set the Circle list again - it's already been set once.");
                }
            }
        }


        public override Circle SelectedObject
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
            FlatRedBall.Utilities.StringFunctions.MakeNameUnique<Circle>(SelectedObject, mEditorCircleList);
        }


        #endregion

        #region Methods

        public CirclePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();

            IncludeMember("X", "Basic");
            IncludeMember("Y", "Basic");

            IncludeMember("Radius", "Basic");
            ((UpDown)GetUIElementForMember("Radius")).MinValue = 0;

            IncludeMember("Color", "Basic");

            IncludeMember("Visible", "Basic");

            IncludeMember("Name", "Basic");

            IncludeMember("RelativeX", "Relative");
            IncludeMember("RelativeY", "Relative");


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
