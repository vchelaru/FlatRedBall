using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;

namespace EditorObjects.Gui
{
    public class SpherePropertyGrid : PropertyGrid<Sphere>
    {
        #region Fields

        PositionedObjectList<Sphere> mEditorSphereList;
        bool IsFirstTimeCalled = true;
        #endregion

        #region Properties
       
        public PositionedObjectList<Sphere> EditorSphereList
        {
            set
            {
                if (mEditorSphereList == null)
                {
                    mEditorSphereList = value;

                    SetMemberChangeEvent("Name", MakeSelectedObjectNameUnique);
                }
                else
                {
                    throw new ArgumentException("Can't set the Sphere list again - it's already been set once.");
                }
            }
        }

        public override Sphere SelectedObject
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
            FlatRedBall.Utilities.StringFunctions.MakeNameUnique<Sphere>(SelectedObject, mEditorSphereList);
        }

        #endregion
        
        #region Methods

        public SpherePropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();

            IncludeMember("X", "Basic");
            IncludeMember("Y", "Basic");
            IncludeMember("Z", "Basic");

            IncludeMember("Radius", "Basic");
            ((UpDown)GetUIElementForMember("Radius")).MinValue = 0;

            IncludeMember("Color", "Basic");

            IncludeMember("Visible", "Basic");

            IncludeMember("Name", "Basic");

            IncludeMember("RelativeX", "Relative");
            IncludeMember("RelativeY", "Relative");
            IncludeMember("RelativeZ", "Relative");

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


