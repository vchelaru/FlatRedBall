using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using System;

namespace FlatRedBall.Glue.CodeGeneration
{
    [Obsolete("This is similar to FullFileCodeGenerator. Use that instead. You will need to implement namespace and class blocks if migrating there.")]
    public abstract class EntireClassCodeGenerator
    {
        #region Properties

        public abstract string ClassName
        {
            get;
        }

        public abstract string Namespace
        {
            get;
        }

        public FilePath ProjectSpecificFullFileName
        {
            get
            {
                string namespaceDirectory = Namespace.Replace(".", "/") + "/";



                return GlueState.Self.CurrentGlueProjectDirectory + namespaceDirectory + ClassName + ".Generated.cs";

            }
        }

        #endregion

        #region Methods
     
        public void GenerateAndAddToProjectIfNecessary()
        {
            string codeContents = GetCode();

            var absoluteFileName = ProjectSpecificFullFileName;

            try
            {
                GlueCommands.Self.TryMultipleTimes(() =>
                {
                    GlueCommands.Self.FileCommands.SaveIfDiffers(absoluteFileName, codeContents);
                });
            }
            catch(System.Exception e)
            {
                GlueCommands.Self.PrintError("Could not save factory, but will try again next time Glue is restarted:\n" + e);
            }

            if (ProjectManager.ProjectBase != null)
            {
                TaskManager.Self.AddAsync(() =>
                {
                    // Why don't we save here?
                    //GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(absoluteFileName, false);
                    GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(absoluteFileName, true);
                }, $"Creating and adding factory file for {ClassName}");

            }
        }

        public void RemoveSelfFromProject()
        {
            ProjectManager.ProjectBase.RemoveItem(ProjectSpecificFullFileName.FullPath);

            foreach (ProjectBase project in ProjectManager.SyncedProjects)
            {
                project.RemoveItem(ProjectSpecificFullFileName.FullPath);
            }

        }

        public abstract string GetCode();

        #endregion
    }
}
