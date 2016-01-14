using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using EditorObjects;

namespace InstructionEditor
{
    public class ReactiveHud
    {
        #region Fields

        Polygon mCurrentObjectHighlight;

        WorldAxesDisplay mWorldAxesDisplay;

        #endregion

        #region Methods

        #region Constructor

        public ReactiveHud()
        {
            mWorldAxesDisplay = new WorldAxesDisplay();

            mCurrentObjectHighlight = Polygon.CreateRectangle(1,1);
            ShapeManager.AddPolygon(mCurrentObjectHighlight);
            mCurrentObjectHighlight.Visible = false;
        }

        #endregion

        #region Public Methods

        public void Update()
        {
            PositionedObject selectedObject = null;

            if (EditorData.EditorLogic.CurrentSprites.Count != 0)
            {
                selectedObject = EditorData.EditorLogic.CurrentSprites[0];
            }
            else if (EditorData.EditorLogic.CurrentTexts.Count != 0)
            {
                selectedObject = EditorData.EditorLogic.CurrentTexts[0];
            }


            mCurrentObjectHighlight.Visible = selectedObject != null;

            if (selectedObject != null)
            {
                mCurrentObjectHighlight.Position = selectedObject.Position;
                mCurrentObjectHighlight.RotationMatrix = selectedObject.RotationMatrix;

                if (selectedObject is IReadOnlyScalable)
                {
                    IReadOnlyScalable asIScalable = selectedObject as IReadOnlyScalable;

                    float scaleX = asIScalable.ScaleX;
                    float scaleY = asIScalable.ScaleY;

                    mCurrentObjectHighlight.ScaleBy(
                        scaleX / mCurrentObjectHighlight.Points[0].X,
                        scaleY / mCurrentObjectHighlight.Points[0].Y);
                }
            }
        }

        #endregion

        #endregion

    }
}
