using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using FlatRedBall.IO;
using FlatRedBall.Glue.VSHelpers.Projects;

namespace FlatRedBall.Glue.Projects
{
    public class CodeProjectHelper
    {

        public void CreateAndAddPartialCodeFile(string generatedFileName, bool saveFile)
        {
            // Currently unit tests don't deal with projects
#if !TEST
            var existingItem = ProjectManager.ProjectBase.GetItem(generatedFileName);

            if (existingItem == null)
            {
                var item = ProjectManager.ProjectBase.AddCodeBuildItem(generatedFileName);
                string withoutPath = FileManager.RemovePath(generatedFileName);

                int firstPeriod = withoutPath.IndexOf('.');
                string parentFile = withoutPath.Substring(0, firstPeriod);

                ProjectManager.ProjectBase.MakeBuildItemNested(item, parentFile + ".cs");

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

                // Try to save a few times just in case there's a hiccup
                const int numberOfTimesToTry = 4;
                int numberOfTries = 0;
                bool succeeded = false;
                while (numberOfTries < numberOfTimesToTry)
                {
                    try
                    {
                        FileManager.SaveText(
                            "", // this gets generated later
                            absoluteFileName);
                        succeeded = true;
                        break;
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(30);

                        numberOfTries++;
                    }
                }
                if (!succeeded)
                {
                    throw new Exception("Error trying to save file " + absoluteFileName);
                }
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
        public bool AddFileToCodeProjectIfNotAlreadyAdded(ProjectBase project, string fileToAddAbsolute)
        {
            bool wasAdded = false;
            // If the file is absolute, shouldn't we make it relative?  Why are we sending "false" as the argument?
            //if (!project.IsFilePartOfProject(fileToAddAbsolute, BuildItemMembershipType.Any, false))
            if (!project.IsFilePartOfProject(fileToAddAbsolute, BuildItemMembershipType.Any, true))
            {
                wasAdded = AddFileToCodeProject(project, fileToAddAbsolute);
                
            }

            return wasAdded;
        }

        public bool AddFileToCodeProject(ProjectBase project, string fileToAddAbsolute)
        {
            bool wasAdded = false;

            string relativeFileName = FileManager.MakeRelative(
                fileToAddAbsolute,
                FileManager.GetDirectory(project.FullFileName));
            relativeFileName = relativeFileName.Replace("/", "\\");

            if (fileToAddAbsolute.EndsWith(".cs"))
            {
                // This should be a code item
                project.AddCodeBuildItem(relativeFileName);

                wasAdded = true;
            }
            return wasAdded;
        }
    }
}
