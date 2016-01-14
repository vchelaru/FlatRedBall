using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using EditorObjects.Parsing;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Managers;

namespace GumPlugin.Managers
{
    class GumxPropertiesManager
    {
        public bool GetAutoCreateGumScreens()
        {
            var gumRfs = GumProjectManager.Self.GetRfsForGumProject();
            if (gumRfs != null)
            {
                return gumRfs.Properties.GetValue<bool>("AutoCreateGumScreens");
            }
            else
            {
                return false;
            }
        }

        public void HandlePropertyChanged(string propertyChanged)
        {
            if (propertyChanged == "UseAtlases")
            {
                UpdateUseAtlases();
            }
            if(propertyChanged == "AutoCreateGumScreens")
            {
                // Do we need to do anything?
            }

            GlueCommands.Self.GluxCommands.SaveGlux();
        }


        public void UpdateUseAtlases()
        {
            var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

            if (gumRfs != null)
            {
                bool useAtlases = 
                    gumRfs.Properties.GetValue<bool>("UseAtlases");

                FileReferenceTracker.Self.UseAtlases = useAtlases;

                var absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(gumRfs);

                // clear the cache for all screens, components, and standards - because whether we use atlases or not has changed
                var gumFiles = GlueCommands.Self.FileCommands.GetFilesReferencedBy(absoluteFileName, TopLevelOrRecursive.TopLevel);

                foreach (var file in gumFiles)
                {
                    GlueCommands.Self.FileCommands.ClearFileCache(file);
                }

                if (useAtlases == false)
                {
                    // If useAtlases is set to false, then that means that 
                    // a lot of new files need to be added to the project.
                    TaskManager.Self.AddAsyncTask(
                        () =>
                        {
                            ProjectManager.UpdateFileMembershipInProject(gumRfs);
                            GlueCommands.Self.ProjectCommands.SaveProjects();
                        },
                        $"Refreshing files in content project for {gumRfs.Name}"); 
                }
            }
        }

    }
}
