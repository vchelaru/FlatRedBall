using FlatRedBall.Content.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.AnimationEditorForms.CommandsAndState
{
    public class ApplicationEvents : Singleton<ApplicationEvents>
    {
        public event Action AfterZoomChange;
        public event Action WireframePanning;
        public event Action WireframeTextureChange;
        public event Action<string> AchxLoaded;
        public event Action<AxisAlignedRectangleSave> AfterAxisAlignedRectangleChanged;
        public event Action<CircleSave> AfterCircleChanged;
        public event Action AnimationChainsChanged;

        public void RaiseAfterAxisAlignedRectangleChanged(AxisAlignedRectangleSave rectangle) => AfterAxisAlignedRectangleChanged?.Invoke(rectangle);
        public void RaiseAfterCircleChanged(CircleSave circle) => AfterCircleChanged?.Invoke(circle);
        public void RaiseAnimationChainsChanged() => AnimationChainsChanged?.Invoke();

        public void CallAchxLoaded(string newFileName) => AchxLoaded?.Invoke(newFileName);

        public void CallAfterZoomChange() => AfterZoomChange?.Invoke();
        

        public void CallAfterWireframePanning()
        {

            if (WireframePanning != null)
            {
                WireframePanning();
            }
        }

        internal void CallWireframeTextureChange()
        {
            if (WireframeTextureChange != null)
            {
                WireframeTextureChange();
            }
        }
    }
}
