using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall;
using FlatRedBall.Gui.PropertyGrids;

#if FRB_MDX
using Rectangle = System.Drawing.Rectangle;
#else
using Rectangle = Microsoft.Xna.Framework.Rectangle;
#endif

namespace EditorObjects.Gui
{
    public class CameraBoundsPropertyGrid : CameraPropertyGrid
    {
        #region Fields

        EditorObjects.CameraBounds mCameraBounds;

        float mBoundsZ = 0;

        ComboBox mBoundsOptions;

        UpDown mTargetZUpDown;

        #endregion

        #region Properties

        public float BoundsZ
        {
            get { return mBoundsZ; }
            set { mBoundsZ = value; }
        }



        public override bool Visible
        {
            get
            {
                return base.Visible;
            }
            set
            {
                base.Visible = value;
                mCameraBounds.Visible = value;
            }
        }

        #endregion

        #region Event Methods

        void ChangeTargetZ(Window callingWindow)
        {
            mBoundsZ = mTargetZUpDown.CurrentValue;
            UpdateBounds();
        }

        protected override void SelectCameraConfiguration(Window callingWindow)
        {
            base.SelectCameraConfiguration(callingWindow);

            UpdateBounds();

            UpdateDisplayedProperties();
        }

        private void UpdateBoundsEvent(Window callingWindow)
        {
            UpdateBounds();
        }

        #endregion

        #region Methods

        public CameraBoundsPropertyGrid(Camera camera)
            : base(GuiManager.Cursor)
        {
            #region Set "this" properties

            GuiManager.AddWindow(this);
            HasCloseButton = true;
            Name = "Camera Bounds";
            this.SelectedObject = camera;

            this.ExcludeMember("NearClipPlane");
            this.ExcludeMember("FarClipPlane");
            RemoveCategory("Clip Plane");
            
            #endregion

            #region SetMemberChangeEvent to update the bounds on any UI change
            SetMemberChangeEvent("X", new GuiMessage(UpdateBoundsEvent));
            SetMemberChangeEvent("Y", new GuiMessage(UpdateBoundsEvent));
            SetMemberChangeEvent("Z", new GuiMessage(UpdateBoundsEvent));
            SetMemberChangeEvent("Orthogonal", new GuiMessage(UpdateBoundsEvent));
            SetMemberChangeEvent("FieldOfView", new GuiMessage(UpdateBoundsEvent));
            SetMemberChangeEvent("AspectRatio", new GuiMessage(UpdateBoundsEvent));
            SetMemberChangeEvent("OrthogonalWidth", new GuiMessage(UpdateBoundsEvent));
            SetMemberChangeEvent("OrthogonalHeight", new GuiMessage(UpdateBoundsEvent));

            UsePixelCoordinatesClick += UpdateBoundsEvent;
            #endregion

            #region Create the camera bounds

            mCameraBounds = new EditorObjects.CameraBounds(camera);
            mCameraBounds.Visible = true;

            #endregion

            //mBoundsOptions = new ComboBox(mCursor);
            //mBoundsOptions.ScaleX = 7;
            //this.AddWindow(mBoundsOptions, "Size");
            //mBoundsOptions.AddItem("Default 2D");
            //mBoundsOptions.AddItem("Default 3D");
            //SetLabelForWindow(mBoundsOptions, "Setting");

            IncludeMember("Orthogonal", "Size");
            IncludeMember("OrthogonalWidth", "Size");
            IncludeMember("OrthogonalHeight", "Size");
            IncludeMember("FieldOfView", "Size");
            IncludeMember("AspectRatio", "Size");
            ShowDestinationRectangle(true);

            ShowCameraConfigurations("Size");

            IncludeMember("X", "Position");
            IncludeMember("Y", "Position");
            IncludeMember("Z", "Position");

            mTargetZUpDown = new UpDown(mCursor);
            this.AddWindow(mTargetZUpDown, "Position");
            this.SetLabelForWindow(mTargetZUpDown, "Target Z");
            mTargetZUpDown.ValueChanged += new GuiMessage(ChangeTargetZ);
            RemoveCategory("Uncategorized");
            
            UpdateBounds();
        }



        public override void UpdateDisplayedProperties()
        {
            base.UpdateDisplayedProperties();

            if (mCameraBounds != null)
            {
                mCameraBounds.UpdateBounds(0);
            }
        }

        private void UpdateBounds()
        {
            mCameraBounds.UpdateBounds(mBoundsZ);

            if (mCameraBounds.Camera.Orthogonal)
            {
                mCameraBounds.Camera.DestinationRectangle = new Rectangle(0, 0, (int)mCameraBounds.Camera.OrthogonalWidth, (int)mCameraBounds.Camera.OrthogonalHeight);
            }
        }
        #endregion
    }
}
