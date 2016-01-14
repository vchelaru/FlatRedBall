using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueViewOfficialPlugins.Scripting;
using GlueView.Scripting;

namespace GlueView.Facades
{
    public class ScriptingCommands
    {

        public void ApplyScript(string script)
        {


            ScriptParsingPlugin.Self.ApplyLines(script);

        }

        public void ApplyScript(string script, CodeContext context)
        {
            ScriptParsingPlugin.Self.ApplyLines(script, context);

        }

    }
}
