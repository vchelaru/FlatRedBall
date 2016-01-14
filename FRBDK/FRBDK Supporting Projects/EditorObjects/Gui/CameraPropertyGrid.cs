using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Gui.PropertyGrids;

namespace EditorObjects.Gui
{
    public class CameraPropertyGrid : PropertyGrid<Camera>
    {
        #region Fields

        Button mUsePixelCoordinates;


        #endregion

        #region Events

        public event GuiMessage UsePixelCoordinatesClick;

        #endregion

        #region Event Methods

        private void OnUsePixelCoordinatesClicked(Window callingWindow)
        {
            if (mSelectedObject != null)
            {
                mSelectedObject.UsePixelCoordinates(false);
                UpdateDisplayedProperties();

                if (UsePixelCoordinatesClick != null)
                {
                    UsePixelCoordinatesClick(callingWindow);
                }
            }
        }

        protected virtual void SelectCameraConfiguration(Window callingWindow)
        {
            CameraConfiguration cameraConfiguration = (CameraConfiguration)(((ComboBox)callingWindow).SelectedObject);

            switch (cameraConfiguration)
            {
                case CameraConfiguration.Default2D:
                    SelectedObject.Orthogonal = true;
                    SelectedObject.OrthogonalWidth = 800;
                    SelectedObject.OrthogonalHeight = 600;
                    
                    break;

                case CameraConfiguration.Silverlight:
                    SelectedObject.Orthogonal = true;
                    SelectedObject.OrthogonalWidth = 800;
                    SelectedObject.OrthogonalHeight = 600;
                    break;
                case CameraConfiguration.Android2D_320X480:
                    //  320 x 480
                    SelectedObject.Orthogonal = true;
                    SelectedObject.OrthogonalWidth = 480;
                    SelectedObject.OrthogonalHeight = 320;

                    break;
                case CameraConfiguration.Android2D800:
                    //  480 x 800
                    SelectedObject.Orthogonal = true;
                    SelectedObject.OrthogonalWidth = 800;
                    SelectedObject.OrthogonalHeight = 480;

                    break;
                case CameraConfiguration.Android2D854:
                    //  480 x 854
                    SelectedObject.Orthogonal = true;
                    SelectedObject.OrthogonalWidth = 854;
                    SelectedObject.OrthogonalHeight = 480;

                    break;
                case CameraConfiguration.WindowsPhoneWVGA2DTall:

                    //WVGA display 480 × 800 resolution
                    SelectedObject.Orthogonal = true;
                    SelectedObject.OrthogonalWidth = 480;
                    SelectedObject.OrthogonalHeight = 800;
                    break;

                case CameraConfiguration.WindowsPhoneWVGA2DWide:

                    //WVGA display 480 × 800 resolution
                    SelectedObject.Orthogonal = true;
                    SelectedObject.OrthogonalWidth = 800;
                    SelectedObject.OrthogonalHeight = 480;


                    break;
                case CameraConfiguration.WindowsPhone3D:
                    SelectedObject.Orthogonal = false;
                    SelectedObject.FieldOfView = (float)System.Math.PI / 4.0f;
                    SelectedObject.AspectRatio = 480 / 800.0f;

                    break;
            }
        }

        #endregion

        #region Methods

        public CameraPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            #region "this" properties

            HasCloseButton = true;

            #endregion

            #region Excludes

            ExcludeAllMembers();

            #endregion

            #region Includes

            IncludeMember("X");
            IncludeMember("Y");
            IncludeMember("Z");
            IncludeMember("Orthogonal");
            IncludeMember("AspectRatio");
            IncludeMember("OrthogonalWidth");
            IncludeMember("OrthogonalHeight");

            IncludeMember("NearClipPlane", "Clip Plane");
            SetMemberDisplayName("NearClipPlane", "Near");

            IncludeMember("FarClipPlane", "Clip Plane");
            SetMemberDisplayName("FarClipPlane", "Far");

            #endregion

            #region Set conditional member visibility

            SetConditionalMemberVisibility<bool>(
                "Orthogonal",
                FlatRedBall.Instructions.Reflection.Operator.EqualTo,
                true,
                VisibilitySetting.ExcludeOnTrue | VisibilitySetting.IncludeOnFalse,
                "FieldOfView");

            SetConditionalMemberVisibility<bool>(
                "Orthogonal",
                FlatRedBall.Instructions.Reflection.Operator.EqualTo,
                true,
                VisibilitySetting.IncludeOnTrue | VisibilitySetting.ExcludeOnFalse,
                "OrthogonalWidth");

            SetConditionalMemberVisibility<bool>(
                "Orthogonal",
                FlatRedBall.Instructions.Reflection.Operator.EqualTo,
                true,
                VisibilitySetting.IncludeOnTrue | VisibilitySetting.ExcludeOnFalse,
                "OrthogonalHeight");


            #endregion

            #region Create the UsePixelCoordinates button

            mUsePixelCoordinates = new Button(GuiManager.Cursor);
            mUsePixelCoordinates.ScaleX = 7f;
            mUsePixelCoordinates.Text = "Set Pixel Perfect";
            mUsePixelCoordinates.Click += OnUsePixelCoordinatesClicked;
            AddWindow(mUsePixelCoordinates, "Actions");

            #endregion

			#region Set custom FieldOfView properties

			UpDown fieldOfViewUpDown = IncludeMember("FieldOfView") as UpDown;
			fieldOfViewUpDown.MinValue = .000001f; // must be greater than 0
			fieldOfViewUpDown.MaxValue = 3.1415f;

			#endregion

			#region Wrap up "this" property setting

			Y = 50;
            SelectCategory("Uncategorized");

            #endregion

            SetAssociations();

        }

        private void SetAssociations()
        {
            SetPropertyGridTypeAssociation(typeof(System.Drawing.Rectangle), typeof(RectanglePropertyGrid));
        }

        public void MakeFieldOfViewAndAspectRatioReadOnly()
        {
            MakeMemberReadOnly("FieldOfView");
            MakeMemberReadOnly("AspectRatio");
        }

        public void ShowCameraConfigurations(string category)
        {
            ComboBox comboBox = new ComboBox(mCursor);

            comboBox.AddItemsFromEnum(typeof(CameraConfiguration));

            comboBox.ScaleX = 8;

            if (!string.IsNullOrEmpty(category))
            {
                this.AddWindow(comboBox, category);
            }
            else
            {
                this.AddWindow(comboBox);
            }
            comboBox.ItemClick += SelectCameraConfiguration;

            SetLabelForWindow(comboBox, "Configuration:");

            this.MoveWindowToTop(comboBox);
        }

        public void ShowDestinationRectangle(bool canWrite)
        {
            IncludeMember("DestinationRectangle", "Resolution");
            RectanglePropertyGrid rectanglePropertyGrid = new RectanglePropertyGrid(mCursor, this, "DestinationRectangle");
            rectanglePropertyGrid.HasMoveBar = false;
            if (!canWrite)
            {
                rectanglePropertyGrid.Enabled = false;
                this.MakeMemberReadOnly("DestinationRectangle");

            }

            this.ReplaceMemberUIElement("DestinationRectangle", rectanglePropertyGrid);
            this.SetMemberDisplayName("DestinationRectangle", "");
        }

        #endregion

    }
}
