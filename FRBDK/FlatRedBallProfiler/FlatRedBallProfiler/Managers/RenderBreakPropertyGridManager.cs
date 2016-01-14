using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Performance.Measurement;
using FlatRedBallProfiler.Managers;

namespace FlatRedBallProfiler
{
    public class RenderBreakPropertyGridManager : Singleton<RenderBreakPropertyGridManager>
    {
        PropertyGrid mPropertyGrid;
        RenderBreakSaveDisplayer mDisplayer;

        public void Initialize(PropertyGrid propertyGrid)
        {
            mDisplayer = new RenderBreakSaveDisplayer();
            mPropertyGrid = propertyGrid;
            mDisplayer.PropertyGrid = mPropertyGrid;
            mDisplayer.RefreshOnTimer = false;
        }


        public void Show(RenderBreakViewModel renderBreakSave)
        {
            mDisplayer.Instance = renderBreakSave;
            mDisplayer.PropertyGrid.Refresh();
        }
    }
}
