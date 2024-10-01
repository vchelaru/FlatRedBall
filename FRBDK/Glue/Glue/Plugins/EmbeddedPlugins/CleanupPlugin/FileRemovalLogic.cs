using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CleanupPlugin
{
    class FileRemovalLogic
    {
        internal void HandleFileRemoved(GlueElement element, ReferencedFileSave file)
        {
            bool isCsv = file.IsCsvOrTreatedAsCsv;

            if(isCsv)
            {
                // The CSV may be using a custom class, but if so, we don't worry about deleting it here.
                // I think that's handled internally in Glue somewhere. So we're going to see if there's a default
                // generated account and if so, delete it:
                string className = FileManager.RemovePath(FileManager.RemoveExtension(file.Name));
                var fileToRemove = GlueState.Self.CurrentGlueProjectDirectory + "DataTypes/" + className + ".Generated.cs";
                GlueCommands.Self.ProjectCommands.RemoveFromProjects(fileToRemove);
            }
        }
    }
}
