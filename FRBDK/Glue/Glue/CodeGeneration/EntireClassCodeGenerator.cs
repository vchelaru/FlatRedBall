using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.CodeGeneration
{
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

        public string ProjectSpecificFullFileName
        {
            get
            {
                string namespaceDirectory = Namespace.Replace(".", "/") + "/";



                return GlueState.Self.CurrentGlueProjectDirectory + namespaceDirectory + ClassName + ".Generated.cs";

            }
        }

        #endregion

        #region Methods

        public bool IsPartOfProject
        {
            get
            {
                return ProjectManager.ProjectBase.GetItem(ProjectSpecificFullFileName) != null;
            }
        }

        public void AddSelfToProject()
        {
            string absoluteFileName = ProjectSpecificFullFileName;

            ProjectManager.ProjectBase.AddCodeBuildItem(absoluteFileName);
        }

        public void GenerateAndAddToProjectIfNecessary()
        {
            string codeContents = GetCode();

            string absoluteFileName = ProjectSpecificFullFileName;

            try
            {
                GlueCommands.Self.TryMultipleTimes(() =>
                        FileManager.SaveText(codeContents, absoluteFileName));
            }
            catch(System.Exception e)
            {
                GlueCommands.Self.PrintError("Could not save factory, but will try again next time Glue is restarted:\n" + e);
            }

            if (ProjectManager.ProjectBase != null && IsPartOfProject == false)
            {
                AddSelfToProject();
            }
        }

        public void RemoveSelfFromProject()
        {
            ProjectManager.ProjectBase.RemoveItem(ProjectSpecificFullFileName);

            foreach (ProjectBase project in ProjectManager.SyncedProjects)
            {
                project.RemoveItem(ProjectSpecificFullFileName);
            }

        }

        public abstract string GetCode();

        #endregion
    }
}
