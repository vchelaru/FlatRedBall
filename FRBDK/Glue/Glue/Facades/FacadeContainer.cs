using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;

namespace FlatRedBall.Glue.GuiDisplay.Facades
{
    public class FacadeContainer
    {

        static FacadeContainer mSelf = new FacadeContainer();

        public static FacadeContainer Self
        {
            get
            {
                return mSelf;
            }
        }

        public IApplicationSettings ApplicationSettings
        {
            get;
            set;
        }

        public IGlueState GlueState
        {
            get;
            set;
        }

        
    }
}
