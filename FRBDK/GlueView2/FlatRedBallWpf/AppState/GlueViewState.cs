using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Screens;
using GlueView2.Patterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.AppState
{
    public class GlueViewState : Singleton<GlueViewState>
    {
        public GlueProjectSave GlueProject
        {
            get; set;
        }


        public Screen CurrentScreenRuntime
        {
            get;
            set;
        }

    }
}
