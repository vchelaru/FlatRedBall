using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Arrow.Managers;
using FlatRedBall.Glue.GuiDisplay.Facades;

namespace FlatRedBall.Arrow.Glue
{
    public class GlueObjectFinder : IObjectFinder
    {
        public FlatRedBall.Glue.SaveClasses.GlueProjectSave GlueProject
        {
            get
            {
                return ArrowState.Self.CurrentGlueProjectSave;
            }
            set
            {
                // do we do anything here?  I think ArrowState should be the authority, not this
            }
        }

        public FlatRedBall.Glue.SaveClasses.IElement GetIElement(string name)
        {
            return ArrowState.Self.CurrentGlueElement;
        }
    }
}
