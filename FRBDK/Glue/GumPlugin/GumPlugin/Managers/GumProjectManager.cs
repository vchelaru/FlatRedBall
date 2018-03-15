using FlatRedBall.Glue;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GumPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GumPlugin.Managers
{
    public class GumProjectManager : Singleton<GumProjectManager>
    {
        public ReferencedFileSave GetRfsForGumProject()
        {
            var glueProject = GlueState.Self.CurrentGlueProject;

            if (glueProject != null)
            {

                return glueProject.GetAllReferencedFiles().FirstOrDefault
                 (item => FileManager.GetExtension(item.Name) == "gumx");
            }
            else
            {
                return null;
            }
        }

        bool GetIsGumProjectAlreadyInGlueProject()
        {
         
            return GetRfsForGumProject() != null;
        }

        public string GetGumProjectFileName()
        {
            // Let's get all the available Screens:
            ReferencedFileSave gumxRfs = GumProjectManager.Self.GetRfsForGumProject();
            if (gumxRfs != null)
            {
                string fullFileName = GlueCommands.Self.GetAbsoluteFileName(gumxRfs);

                return fullFileName;
            }
            return null;
        }

        public void ReloadProject()
        {
            string gumProjectFileName = GetGumProjectFileName();
            if (!string.IsNullOrEmpty(gumProjectFileName))
            {
                string gumxDirectory = null;

                gumxDirectory = FileManager.GetDirectory(gumProjectFileName);

                FileReferenceTracker.Self.LoadGumxIfNecessaryFromDirectory(gumxDirectory, true);
            }
            else
            {
                // Set the project to null so we don't have code generation happening:
                AppState.Self.GumProjectSave = null;
            }
        }

        internal bool TryAddNewGumProject()
        {
            bool added = false;

            if (GlueState.Self.CurrentGlueProject == null)
            {
                MessageBox.Show("You must first create a Glue project before adding a Gum project");
            }

            else if (GetIsGumProjectAlreadyInGlueProject())
            {
                MessageBox.Show("A Gum project already exists");
            }
            else
            {
                string gumProjectDirectory = GlueState.Self.ContentDirectory + "GumProject/";
                EmbeddedResourceManager.Self.SaveEmptyProject(gumProjectDirectory);

                EditorLogic.CurrentTreeNode = FlatRedBall.Glue.FormHelpers.ElementViewWindow.GlobalContentFileNode;

                bool userCancelled = false;

                // ignore changes while this is being added, because we don't want to add then remove files:


                var rfs = FlatRedBall.Glue.FormHelpers.RightClickHelper.AddSingleFile(
                    gumProjectDirectory + "GumProject.gumx", ref userCancelled);

                rfs.Properties.SetValue(
                    nameof(FileAdditionBehavior), 
                    (int)FileAdditionBehavior.IncludeNoFiles);


                added = !userCancelled;

                if(added)
                {
                    GlueState.Self.CurrentReferencedFileSave = rfs;
                }
            }

            return added;
        }
    }
}
