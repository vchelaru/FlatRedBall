using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Arrow.Managers;
using FlatRedBall.Glue;
using FlatRedBall.Instructions.Reflection;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Arrow.GlueView
{
    public class SelectionManager : Singleton<SelectionManager>
    {
        ElementRuntimeHighlight mSelectionHighlight;


        public void Initialize()
        {
            mSelectionHighlight = new ElementRuntimeHighlight();
            mSelectionHighlight.Color = Color.Red;
        }

        public void UpdateToSelectedElementOrInstance()
        {
            if (ArrowState.Self.CurrentInstance != null)
            {
                mSelectionHighlight.CurrentElement = ArrowState.Self.CurrentContainedElementRuntime;
            }
            else
            {
                mSelectionHighlight.CurrentElement = null;

            }
        }

    }
}
