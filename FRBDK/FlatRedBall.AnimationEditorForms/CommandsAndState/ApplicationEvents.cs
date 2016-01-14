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

        public void CallAfterZoomChange()
        {
            if (AfterZoomChange != null)
            {
                AfterZoomChange();
            }
        }

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
