using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Projects
{
    public class CodeProjectHelper
    {

        public void CreateAndAddPartialGeneratedCodeFile(string generatedFileName, bool saveFile)
        {
            // Currently unit tests don't deal with projects
#if !TEST
            var existingItem = ProjectManager.ProjectBase.GetItem(generatedFileName);

            if (existingItem == null)
            {
                var item = GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(generatedFileName, save: false);
                string withoutPath = FileManager.RemovePath(generatedFileName);

                int firstPeriod = withoutPath.IndexOf('.');
                string parentFile = withoutPath.Substring(0, firstPeriod);

                ProjectManager.ProjectBase.MakeBuildItemNested(item, parentFile + ".cs");

                // This used to not save the main project, not sure why...
                GlueCommands.Self.ProjectCommands.SaveProjects();

                // todo:  Gotta get this working on synced projects
            }
#endif

            if (saveFile)
            {
                string absoluteFileName = generatedFileName;

                if (FileManager.IsRelative(generatedFileName))
                {
                    absoluteFileName = FileManager.RelativeDirectory + generatedFileName;
                }

                FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile(absoluteFileName);

                GlueCommands.Self.TryMultipleTimes(() =>
                {
                    // this gets generated later
                    FileManager.SaveText("", absoluteFileName);
                });
                
            }
        }

        /// <summary>
        /// Adds the argument fileToAddAbsolute to the argument projectBase.  This function does not
        /// save the project nor does it modify synced projects.
        /// </summary>
        /// <param name="project">The project to add the file to.</param>
        /// <param name="fileToAddAbsolute">The file to add.</param>
        /// <returns>Whether the file was added.  This may be false if the file really is not a code file
        /// or if the file has already been added to the project.</returns>
        public bool AddFileToCodeProjectIfNotAlreadyAdded(VisualStudioProject project, string fileToAddAbsolute)
        {
            if(project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
            bool wasAdded = false;
            // If the file is absolute, shouldn't we make it relative?  Why are we sending "false" as the argument?
            //if (!project.IsFilePartOfProject(fileToAddAbsolute, BuildItemMembershipType.Any, false))
            if (!project.IsFilePartOfProject(fileToAddAbsolute, BuildItemMembershipType.Any, true))
            {
                wasAdded = AddFileToCodeProject(project, fileToAddAbsolute);
                
            }

            return wasAdded;
        }

        public bool AddFileToCodeProject(VisualStudioProject project, string fileToAddAbsolute)
        {
            bool wasAdded = false;

            string relativeFileName = FileManager.MakeRelative(
                fileToAddAbsolute,
                FileManager.GetDirectory(project.FullFileName.FullPath));
            relativeFileName = relativeFileName.Replace("/", "\\");

            if (fileToAddAbsolute.EndsWith(".cs"))
            {
                // This should be a code item
                ((VisualStudioProject)project.CodeProject).AddCodeBuildItem(relativeFileName);

                wasAdded = true;
            }
            else if(fileToAddAbsolute.EndsWith(".dll"))
            {
                project.AddContentBuildItem(relativeFileName);
            }
            return wasAdded;
        }
    }
}
