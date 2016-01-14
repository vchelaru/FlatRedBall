using FlatRedBall.Math.Splines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolTemplate;

namespace SplineEditor.States
{
    public class AppState : Singleton<AppState>
    {
        public PreviewState Preview
        {
            get;
            private set;
        }

        public Spline CurrentSpline
        {
            get
            {
                return EditorData.EditorLogic.CurrentSpline;
            }
            set
            {
                EditorData.EditorLogic.CurrentSpline = value;
            }
        }

        public SplinePoint CurrentSplinePoint
        {
            get
            {
                return EditorData.EditorLogic.CurrentSplinePoint;
            }
            set
            {
                EditorData.EditorLogic.CurrentSplinePoint = value;
            }
        }

        public AppState()
        {
            Preview = new PreviewState();
        }
    }
}
