using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using EditorObjects;
using ToolTemplate.Entities;
using FlatRedBall.Math.Splines;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using SplineEditor.States;
using SplineEditor.Entities;
namespace ToolTemplate
{
    public class ReactiveHud
    {
        #region Fields

        static CameraBounds mCameraBounds;
        static SplineMover mSplineMover;
        static SplinePointSelectionMarker mCurrentSplinePointMarker;

        static ReactiveHud mSelf;
        #endregion

        #region Properties

        public static ReactiveHud Self
        {
            get
            {
                return mSelf;
            }
        }

        public CameraBounds CameraBounds
        {
            get { return mCameraBounds; }
        }

        public SplineMover SplineMover
        {
            get { return mSplineMover; }
        }

        public SplinePointSelectionMarker SplinePointSelectionMarker
        {
            get { return mCurrentSplinePointMarker; }
        }

        #endregion

        #region Methods

        public ReactiveHud()
        {
            mCameraBounds = new CameraBounds(EditorData.BoundsCamera);

            mSplineMover = new SplineMover();

            mCurrentSplinePointMarker = new SplinePointSelectionMarker();

            mSelf = this;
        }

        public void Update()
        {
            mCameraBounds.UpdateBounds(0);

            mSplineMover.Activity();

            UpdateCurrentSplinePointMarker();
        }

        private void UpdateCurrentSplinePointMarker()
        { 
            SplinePoint currentPoint = AppState.Self.CurrentSplinePoint;

            mCurrentSplinePointMarker.UpdateToSplinePoint(currentPoint);
        }

        #endregion
    }
}
