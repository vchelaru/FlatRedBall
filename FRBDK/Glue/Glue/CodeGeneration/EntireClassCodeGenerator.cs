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

            const int numberOfTimesToTry = 5;

            int numberOfFailures = 0;
            bool succeeded = false;
            while (numberOfFailures < numberOfTimesToTry)
            {
                try
                {
                    FileManager.SaveText(codeContents, absoluteFileName);

                    succeeded = true;
                    break;
                }
                catch
                {
                    numberOfFailures++;
                }

            }

            if (!succeeded)
            {
                GlueGui.ShowMessageBox("Failed to generate factory at file:\n\n" +
                    absoluteFileName + "\n\nIs the file being locked by something?\n" +
                    "This is not a fatal error - you can manually re-generate this " +
                    "object or make a change to it to force a regeneration.");
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
