using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.GuiDisplay.Facades
{
    public interface IObjectFinder
    {
        GlueProjectSave GlueProject
        {
            get;
            set;
        }
  
    }
}
