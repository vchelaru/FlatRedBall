using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FlatRedBall.Glue
{
    [Description("SelectionInterface")]
    public class SelectionInterface: MarshalByRefObject
    {
        List<string> messages = new List<string>();

        string mLastElementShown;

        bool ShowDebugInfo = false;

        public void LoadGluxFile(string file)
        {
            if (ShowDebugInfo)
            {
                FlatRedBall.Debugging.Debugger.CommandLineWrite("Loading Glux: " + file);
            }
            GluxManager.LoadGlux(file);
        }

        
        public void UnloadGlux()
        {
            if (ShowDebugInfo)
            {
                FlatRedBall.Debugging.Debugger.CommandLineWrite("Unloading Glux");
            }
            GluxManager.UnloadGlux(); 
        }

        public void RefreshGlueProject()
        {
             
            if (ShowDebugInfo)
            {

                FlatRedBall.Debugging.Debugger.CommandLineWrite("Refresh Glue Project");
            }
            bool onlyReloadVariables = false; 
            GluxManager.RefreshGlueProject(onlyReloadVariables); 
        }

        public void RefreshFile(string fileName)
        {
            if (ShowDebugInfo)
            {
                FlatRedBall.Debugging.Debugger.CommandLineWrite("Refreshing file " + fileName);
            }

            GluxManager.RefreshFile(fileName);
        }

        public void ShowElement(string name)
        {
            if (ShowDebugInfo)
            {
                FlatRedBall.Debugging.Debugger.CommandLineWrite("Show element " + name);
            }
            mLastElementShown = name;

            if (GluxManager.CurrentElementName != name)
            {
                GluxManager.ShowElement(name);
            }
        }

        public void HighlightElement(string name)
        {
            
            if (ShowDebugInfo)
            {
                FlatRedBall.Debugging.Debugger.CommandLineWrite("Highlight Element " + name);
            }
            GluxManager.ElementToHighlight = name; 

        }

        public void SetState(string name)
        {
            if (GluxManager.CurrentElement != null)
            {
                
                if (ShowDebugInfo)
                {
                    FlatRedBall.Debugging.Debugger.CommandLineWrite("Set State " + name);
                }
                GluxManager.ShowState(name);
            }
        }

        public void RefreshCurrentElement()
        {
            if (GluxManager.CurrentElement != null)
            {
                GluxManager.ShowElement(GluxManager.CurrentElement.Name); 
            }

        }

        public void RefreshVariables()
        {
            bool onlyReloadVariables = true; 
            GluxManager.RefreshGlueProject(onlyReloadVariables); 
            
        }

        public bool IsConnected()
        {
            return true; 
        }

        public void ExecuteScript(string script)
        {
            GluxManager.ReceiveScript(script);
        }
    }
}
