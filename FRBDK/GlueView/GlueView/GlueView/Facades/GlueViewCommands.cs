using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueView.Forms;

namespace GlueView.Facades
{
    public class GlueViewCommands
    {
        static GlueViewCommands mSelf;

        public static GlueViewCommands Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new GlueViewCommands();
                }
                return mSelf;
            }
        }

        public CollapsibleFormHelper CollapsibleFormCommands
        {
            get
            {
                return CollapsibleFormHelper.Self;
            }
        }

        public ScriptingCommands ScriptingCommands
        {
            get;
            set;
        }
        public ElementCommands ElementCommands
        {
            get;
            private set;
        }

		public GlueProjectSaveCommands GlueProjectSaveCommands
		{
			get;
			private set;
		}


        public GlueViewCommands()
        {
            ElementCommands = new ElementCommands();
			GlueProjectSaveCommands = new GlueProjectSaveCommands();
            ScriptingCommands = new ScriptingCommands();
        }
    }
}
