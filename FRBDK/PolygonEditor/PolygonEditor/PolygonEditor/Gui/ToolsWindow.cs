using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Gui;

#if FRB_MDX

#else
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

namespace PolygonEditor.Gui
{
    public class ToolsWindow : EditorObjects.Gui.ToolsWindow
    {
        #region Fields

        ToggleButton mMoveButton;
        ToggleButton mRotateButton;
        ToggleButton mScaleButton;

        ToggleButton mAddPointButton;
        ToggleButton mDrawPolygonToggleButton;

        #endregion

        #region Properties

        public bool IsAddPointButtonPressed
        {
            get { return mAddPointButton.IsPressed; }
        }

        public bool IsDrawingPolygonButtonPressed
        {
            get { return mDrawPolygonToggleButton.IsPressed; }
        }

        public bool IsMoveButtonPressed
        {
            get { return mMoveButton.IsPressed; }
        }

        public bool IsRotateButtonPressed
        {
            get { return mRotateButton.IsPressed; }
        }

        public bool IsScaleButtonPressed
        {
            get { return mScaleButton.IsPressed; }
        }


        #endregion

        #region Methods

        public ToolsWindow()
            : base()
        {
            #region Move button

            this.mMoveButton = AddToggleButton(Keys.M);
            this.mMoveButton.Text = "Move";
            this.mMoveButton.SetOverlayTextures(2, 0);

            #endregion

            #region Scale button

            this.mScaleButton = AddToggleButton(Keys.X);
            this.mScaleButton.Text = "Scale";
            this.mMoveButton.AddToRadioGroup(this.mScaleButton);
            this.mScaleButton.SetOverlayTextures(1, 0);

            #endregion

            #region Rotate Btton

            this.mRotateButton = AddToggleButton(Keys.R);
            this.mRotateButton.Text = "Rotate";
            this.mMoveButton.AddToRadioGroup(this.mRotateButton);
            this.mRotateButton.SetOverlayTextures(0, 0);

            #endregion

            #region Add Point to current Polygon

            mAddPointButton = AddToggleButton(Keys.A);
            mAddPointButton.Text = "Add Point";
            mMoveButton.AddToRadioGroup(mAddPointButton);
            mAddPointButton.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>(@"Assets\UI\AddPoint.png"), null);

            #endregion

            #region Draw Polygon

            mDrawPolygonToggleButton = AddToggleButton();
            mDrawPolygonToggleButton.Text = "Draw new Polygon";
            mMoveButton.AddToRadioGroup(mDrawPolygonToggleButton);
            mDrawPolygonToggleButton.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>(@"Assets\UI\DrawPolygon.png"), null);


            #endregion

            this.X = SpriteManager.Camera.XEdge * 2 - this.ScaleX;
            this.Y = 9.0f;
        }

        public void Update()
        {
            // Update the Add Point button Enable
            mAddPointButton.Enabled = EditorData.EditingLogic.CurrentPolygons.Count != 0;
        }

        #endregion

    }
}
