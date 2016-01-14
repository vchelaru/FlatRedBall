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
    public class GeometryWindow : EditorObjects.Gui.ToolsWindow
    {
        #region Enum
            public enum GeometryButton
            {
                AxisAlignedRectangle,
                Circle,
                RectanglePolygon,
                AxisAlignedCube,
                Sphere
            }
        #endregion

        #region Fields
            Button mAxisAlignedRectangleButton;
            Button mCircleButton;
            Button mRectanglePolygonButton;
            Button mAxisAlignedCubeButton;
            Button mSphereButton;

            ToggleButton mEditAxisAlignedRectangleButton;
            ToggleButton mEditCircleButton;
            ToggleButton mEditPolygonButton;
            ToggleButton mEditAxisAlignedCubeButton;
            ToggleButton mEditSphereButton;
        #endregion

        #region Properties

            public bool EditingAxisAlignedRectangles
            {
                get { return mEditAxisAlignedRectangleButton.IsPressed; }
            }

            public bool EditingCircles
            {
                get { return mEditCircleButton.IsPressed; }
            }

            public bool EditingPolygons
            {
                get { return mEditPolygonButton.IsPressed; }
            }

            public bool EditingAxisAlignedCubes
            {
                get { return mEditAxisAlignedCubeButton.IsPressed; }
            }

            public bool EditingSpheres
            {
                get { return mEditSphereButton.IsPressed; }
            }

        #endregion

        #region Events
            public void AddAxisAlignedRectangle(Window callingWindow)
            {
                EditorData.EditingLogic.AddAxisAlignedRectangle();
            }

            public void AddAxisAlignedCube(Window callingWindow)
            {
                EditorData.EditingLogic.AddAxisAlignedCube();
            }

            public void AddCircle(Window callingWindow)
            {
                EditorData.EditingLogic.AddCircle();
            }
        
            public void AddSphere(Window callingWindow)
            {
                EditorData.EditingLogic.AddSphere();
            }

            public void AddRectanglePolygon(Window callingWindow)
            {
                EditorData.EditingLogic.AddRectanglePolygon();
            }            
        #endregion

        #region Methods

            /*
             * This is the basic constructor for the GeometryWindow. It uses ToggleButtons instead of Buttons for a functionality aspect.
             * The desire was to have a single click on the button create 1 geometric shape, and then have the button
             * reset itself to the off position. The Button class may be used, but as of August 2nd, 2008, I don't believe has the
             * support for the desired functionality - Aaron
            */
            public GeometryWindow()
                : base()
            {
                base.mName = "Geometry";
                NumberOfRows = 5;

                this.X = SpriteManager.Camera.XEdge * 2 - this.ScaleX;
                this.Y = 19.0f;

                this.ExtraSpacingBetweenSameRowButtons = .2f;

                #region Add AxisAlignedRectangle
                this.mAxisAlignedRectangleButton = AddButton(Keys.P);
                this.mAxisAlignedRectangleButton.Text = "AxisAlignedRectangle (P)";
                mAxisAlignedRectangleButton.SetOverlayTextures(FlatRedBallServices.Load<Texture2D>(@"Assets\UI\AxisAlignedRectangle.png"), null);
                mAxisAlignedRectangleButton.Click += AddAxisAlignedRectangle;
                #endregion

                #region Add Circle
                this.mCircleButton = AddButton(Keys.O);
                this.mCircleButton.Text = "Circle (O)";
                mCircleButton.SetOverlayTextures(FlatRedBallServices.Load<Texture2D>(@"Assets\UI\Circle.png"), null);
                mCircleButton.Click += AddCircle;
                #endregion

                #region Add Polygon (Rectangle)

                this.mRectanglePolygonButton = AddButton(Keys.I);
                this.mRectanglePolygonButton.Text = "Rectangle Polygon (I)";
                mRectanglePolygonButton.SetOverlayTextures(FlatRedBallServices.Load<Texture2D>(@"Assets\UI\RectanglePolygon.png"), null);
                mRectanglePolygonButton.Click += AddRectanglePolygon;

                #endregion

                #region Add AxisAlignedCube

                this.mAxisAlignedCubeButton = AddButton(Keys.U);
                this.mAxisAlignedCubeButton.Text = "AxisAlignedCube (U)";
                mAxisAlignedCubeButton.SetOverlayTextures(FlatRedBallServices.Load<Texture2D>(@"Assets\UI\AxisAlignedCube.png"), null);
                mAxisAlignedCubeButton.Click += AddAxisAlignedCube;

                #endregion

                #region Add Sphere

                this.mSphereButton = AddButton(Keys.Y);
                this.mSphereButton.Text = "Sphere (Y)";
                mSphereButton.SetOverlayTextures(FlatRedBallServices.Load<Texture2D>(@"Assets\UI\Sphere.png"), null);
                mSphereButton.Click += AddSphere;

                #endregion

                #region Edit AxisAlignedRectangle

                mEditAxisAlignedRectangleButton = AddToggleButton();
                mEditAxisAlignedRectangleButton.SetText("Currently NOT editing Axis Aligned Rectangles", "Currently editing Axis Aligned Rectangles");
                Texture2D texture = FlatRedBallServices.Load<Texture2D>(@"Assets\UI\EditAxisAlignedRectangle.png");
                mEditAxisAlignedRectangleButton.SetOverlayTextures(
                    FlatRedBallServices.Load<Texture2D>(@"Assets\UI\NoEditAxisAlignedRectangle.png"),
                    texture
                    );
                mEditAxisAlignedRectangleButton.Press();
                #endregion

                #region Edit Circle
                mEditCircleButton = AddToggleButton();
                mEditCircleButton.SetText("Currently NOT editing Circles", "Currently editing Circles");
                texture = FlatRedBallServices.Load<Texture2D>(@"Assets\UI\EditCircle.png");
                mEditCircleButton.SetOverlayTextures(FlatRedBallServices.Load<Texture2D>(@"Assets\UI\NoEditCircle.png"), texture);
                mEditCircleButton.Press();
                #endregion

                #region Edit Polygon
                mEditPolygonButton = AddToggleButton();
                mEditPolygonButton.SetText("Currently NOT editing Polygons", "Currently editing Polygons");
                texture = FlatRedBallServices.Load<Texture2D>(@"Assets\UI\EditPolygon.png");
                mEditPolygonButton.SetOverlayTextures(FlatRedBallServices.Load<Texture2D>(@"Assets\UI\NoEditPolygon.png"), texture);
                mEditPolygonButton.Press();
                #endregion

                #region Edit AxisAlignedCube
                mEditAxisAlignedCubeButton = AddToggleButton();
                mEditAxisAlignedCubeButton.SetText("Currently NOT editing Axis Aligned Cubes", "Currently editing Axis Aligned Cubes");
                texture = FlatRedBallServices.Load<Texture2D>(@"Assets\UI\EditAxisAlignedCube.png");
                mEditAxisAlignedCubeButton.SetOverlayTextures(FlatRedBallServices.Load<Texture2D>(@"Assets\UI\NoEditAxisAlignedCube.png"), texture);
                mEditAxisAlignedCubeButton.Press();
                #endregion

                #region Edit Sphere
                mEditSphereButton = AddToggleButton();
                mEditSphereButton.SetText("Currently NOT editing Spheres", "Currently editing Spheres");
                texture = FlatRedBallServices.Load<Texture2D>(@"Assets\UI\EditSphere.png");
                mEditSphereButton.SetOverlayTextures(FlatRedBallServices.Load<Texture2D>(@"Assets\UI\NoEditSphere.png"), texture);
                mEditSphereButton.Press();
                #endregion

            }

            public void Update()
            {
                
            }

        #endregion

    }
}
