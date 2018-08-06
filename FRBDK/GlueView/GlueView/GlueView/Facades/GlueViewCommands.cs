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

        public ElementCommands ElementCommands
        {
            get;
            private set;
        }

        public FileCommands FileCommands
        {
            get; private set;
        }


        public GlueProjectSaveCommands GlueProjectSaveCommands
		{
			get;
			private set;
		}

        public ScriptingCommands ScriptingCommands
        {
            get;
            set;
        }

        public void PrintOutput(string output)
        {
            Wcf.WcfManager.Self.PrintOutput(output);
        }


        public GlueViewCommands()
        {
            ElementCommands = new ElementCommands();
            FileCommands = new FileCommands();
			GlueProjectSaveCommands = new GlueProjectSaveCommands();
            ScriptingCommands = new ScriptingCommands();
        }
    }
}
