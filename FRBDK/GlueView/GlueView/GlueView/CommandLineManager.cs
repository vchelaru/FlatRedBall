using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueView.Forms;
using GlueView.Managers;
using GlueViewOfficialPlugins.Scripting;

namespace GlueView
{
    public class CommandLineManager : Singleton<CommandLineManager>
    {
        public void ProcessCommandLineArgs(ToolForm toolForm)
        {
            foreach (var argument in Program.CommandLineArgs)
            {
                if (argument == "ScriptMode")
                {
                    SwitchToScriptMode(toolForm);
                }
            }
        }

        private void SwitchToScriptMode(ToolForm toolForm)
        {
            var foundForm = CollapsibleFormHelper.Self.GetControlByLabel("Script Parsing") as ScriptingControl;

            if (foundForm != null)
            {

                foundForm.ShowNewFullScriptingForm();
                // need to hide the window
                toolForm.Minimize();
            }
        
        }
    }
}
