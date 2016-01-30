using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    public class SelectCommands : ISelectCommands
    {
        public void Select(ReferencedFileSave referencedFile, string objectInFile = null)
        {
            // first let's select it:
            GlueState.Self.CurrentReferencedFileSave = referencedFile;

            // Next lets tell the plugin manager to try to find this object:
            if( string.IsNullOrEmpty(objectInFile) == false)
            {
                PluginManager.SelectItemInCurrentFile(objectInFile);
            }
        }
    }
}
