using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using EditorObjects;
using ParticleEditor.Entities;
#if FRB_XNA
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using Color = Microsoft.Xna.Framework.Graphics.Color;
#elif FRB_MDX
using Color = System.Drawing.Color;
#endif

namespace ParticleEditor
{
    public class ReactiveHud
    {
        #region Fields

        private Sprite mCurrentEmitterMarker;

        private EmissionAreaVisibleRepresentation mEmissionAreaVisibleRepresentation;

        private Layer mMarkerLayer;

        private Sprite mSpriteOverMarker;

        private PositionedObjectList<AxisAlignedRectangle> mCurrentEmitterBoundaryCorners = new PositionedObjectList<AxisAlignedRectangle>();

        #endregion

        #region Properties

        public Sprite CurrentEmitterMarker
        {
            get { return mCurrentEmitterMarker; }
        }

        public PositionedObjectList<AxisAlignedRectangle> CurrentEmitterBoundaryCorners
        {
            get { return mCurrentEmitterBoundaryCorners; }
        }

        #endregion

        #region Methods

        #region Constructor

        public ReactiveHud()
        {			

            mMarkerLayer = SpriteManager.AddLayer();

            #region Create the Sprite Over Marker

            mSpriteOverMarker = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/smallSquareClear.bmp", AppState.Self.PermanentContentManager),
                mMarkerLayer);
            mSpriteOverMarker.RelativeZ = -.0001f;
            mSpriteOverMarker.Visible = false;
            mSpriteOverMarker.Alpha = 100;                

			#endregion

            #region Create the Current Emitter Marker

            mCurrentEmitterMarker = SpriteManager.AddSprite(
                FlatRedBallServices.Load<Texture2D>("Content/smallSquare.bmp", AppState.Self.PermanentContentManager),
                mMarkerLayer);
            mCurrentEmitterMarker.Visible = false;

            #endregion

            mEmissionAreaVisibleRepresentation = new EmissionAreaVisibleRepresentation();
        }

        #endregion

        #region Public Methods

        public void Update()
        {
            #region Update Current Emitter Marker
            if (AppState.Self.CurrentEmitter == null)
            {
                mCurrentEmitterMarker.Visible = false;
                foreach (AxisAlignedRectangle rectangle in mCurrentEmitterBoundaryCorners)
                {
                    rectangle.Visible = false;
                }
            }
            else
            {
                mCurrentEmitterMarker.Visible = true;
                foreach (AxisAlignedRectangle rectangle in mCurrentEmitterBoundaryCorners)
                {
                    rectangle.Visible = AppState.Self.CurrentEmitter.BoundedEmission;
                }

                mCurrentEmitterMarker.X = AppState.Self.CurrentEmitter.X;
                mCurrentEmitterMarker.Y = AppState.Self.CurrentEmitter.Y;
                mCurrentEmitterMarker.Z = AppState.Self.CurrentEmitter.Z;

                mCurrentEmitterMarker.ScaleX = 5 / SpriteManager.Camera.PixelsPerUnitAt(AppState.Self.CurrentEmitter.Z);
                mCurrentEmitterMarker.ScaleY = mCurrentEmitterMarker.ScaleX;

                #region Update emission boundary positions and scales

                UpdateCornerRectangleCount();

                UpdateCornerRectanglePositions();

                UpdateCornerRectangleScales();

                #endregion
            }
            #endregion

            #region Update the SpriteOverMarker

            if (EditorData.EditorLogic.SceneSpriteOver != null)
            {
                if (mSpriteOverMarker.Parent != EditorData.EditorLogic.SceneSpriteOver)
                {
                    mSpriteOverMarker.AttachTo(EditorData.EditorLogic.SceneSpriteOver, false);
                }

                mSpriteOverMarker.Visible = true;

                mSpriteOverMarker.ScaleX = EditorData.EditorLogic.SceneSpriteOver.ScaleX;
                mSpriteOverMarker.ScaleY = EditorData.EditorLogic.SceneSpriteOver.ScaleY;
            }
            else
            {
                mSpriteOverMarker.Detach();
                mSpriteOverMarker.Visible = false;
            }

            #endregion

            mEmissionAreaVisibleRepresentation.UpdateToEmitter(AppState.Self.CurrentEmitter);

        }

        #endregion

        #region Private Methods

        private void UpdateCornerRectangleCount()
        {
            int numberOfEdges = AppState.Self.CurrentEmitter.EmissionBoundary.Points.Count - 1; // assume the last point repeats

            while (mCurrentEmitterBoundaryCorners.Count < numberOfEdges)
            {
                AxisAlignedRectangle newRectangle = ShapeManager.AddAxisAlignedRectangle();
                newRectangle.Color = Color.Red;

                newRectangle.ScaleX = newRectangle.ScaleY = .3f;

                mCurrentEmitterBoundaryCorners.Add(newRectangle);
            }

            while (mCurrentEmitterBoundaryCorners.Count > numberOfEdges)
            {
                ShapeManager.Remove(mCurrentEmitterBoundaryCorners[mCurrentEmitterBoundaryCorners.Count - 1]);

            }
        }

        private void UpdateCornerRectanglePositions()
        {
            Polygon polygon = AppState.Self.CurrentEmitter.EmissionBoundary;
            for (int i = 0; i < polygon.Points.Count - 1; i++)
            {
                if (mCurrentEmitterBoundaryCorners[i].Parent != polygon)
                {
                    mCurrentEmitterBoundaryCorners[i].AttachTo(polygon, false);
                }

                mCurrentEmitterBoundaryCorners[i].RelativeX = (float)(polygon.Points[i].X);
                mCurrentEmitterBoundaryCorners[i].RelativeY = (float)(polygon.Points[i].Y);
            }
        }

        private void UpdateCornerRectangleScales()
        {
            for (int i = 0; i < AppState.Self.CurrentEmitter.EmissionBoundary.Points.Count - 1; i++)
            {
                // Set scale to be screensize when viewed at camera distance

                float pixelsPerUnit = Math.Abs(SpriteManager.Camera.PixelsPerUnitAt(mCurrentEmitterBoundaryCorners[i].Z));

                mCurrentEmitterBoundaryCorners[i].ScaleX = 5 * (1 / pixelsPerUnit);
                mCurrentEmitterBoundaryCorners[i].ScaleY = 5 * (1 / pixelsPerUnit);
            }
        }

        #endregion


        #endregion
    }
}
