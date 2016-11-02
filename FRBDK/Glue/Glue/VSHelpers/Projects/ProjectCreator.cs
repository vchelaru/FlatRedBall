using System;
using System.Linq;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public static class ProjectCreator
    {
        public static ProjectBase LoadXnaProjectFor(ProjectBase masterProject, string fileName)
        {
            ProjectBase project = CreateProject(fileName);

            project.Load(fileName);

            project.MasterProjectBase = masterProject;

            project.IsContentProject = true;

            return project;

        }


        public static ProjectBase CreateProject(string fileName)
        {
            //Project coreVisualStudioProject = new Project(fileName);
            Project coreVisualStudioProject = new Project(fileName, null, null, new ProjectCollection());

            ProjectBase toReturn = CreatePlatformSpecificProject(coreVisualStudioProject, fileName);

#if GLUE
            // It may be null if the project is of an unknown type.  
            // We'll handle that problem outside of this function.
            if (toReturn != null)
            {
                toReturn.Saving += FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile;
            }
#endif
            return toReturn;
        }

        public static ProjectBase CreatePlatformSpecificProject(Project coreVisualStudioProject, string fileName)
        {
            ProjectBase toReturn = null;
            if (FileManager.GetExtension(fileName) == "contentproj")
            {
                toReturn = new XnaContentProject(coreVisualStudioProject);
            }


            if (toReturn == null)
            {
                toReturn = TryGetProjectTypeFromDefineConstants(coreVisualStudioProject);
            }


            if (toReturn == null)
            {
                // If we got here that means that the preprocessor defines don't match what
                // Glue expects.  This is probably bad - Glue generated code will likely not
                // compile, so let's warn the user
                string warning = "Could not determine project type based off of preprocessor defines.  Glue will try to load the project, but you may have compilation errors";
                GlueGui.ShowMessageBox(warning);


                #region Backup Method for detecting project type off of FlatRedBall

                foreach (ProjectItem buildItem in coreVisualStudioProject.AllEvaluatedItems.Where(buildItem => buildItem.EvaluatedInclude.Contains("FlatRedBall")))
                {
                    if (buildItem.EvaluatedInclude.Contains("Mdx"))
                    {
                        toReturn = new MdxProject(coreVisualStudioProject);
                        break;
                    }

                    if (buildItem.EvaluatedInclude.Contains("FlatRedBall"))
                    {
                        if (buildItem.EvaluatedInclude.Contains("x86"))
                        {
                            toReturn = new XnaProject(coreVisualStudioProject);
                            break;
                        }
                        
                    }

                    break;
                }
                #endregion
            }

            if (toReturn == null)
            {

                foreach (ProjectItem buildItem in coreVisualStudioProject.AllEvaluatedItems.Where(buildItem => buildItem.EvaluatedInclude.Contains("Microsoft.Phone")))
                {
                    toReturn = new WindowsPhoneProject(coreVisualStudioProject);
                    break;
                }

            }


            return toReturn;
        }

        private static ProjectBase TryGetProjectTypeFromDefineConstants(Project coreVisualStudioProject)
        {
            string preProcessorConstants = GetPreProcessorConstantsFromProject(coreVisualStudioProject);

            //string sasfd = ProjectManager.LibrariesPath;

            // Check for other platforms before checking for FRB_XNA because those projects
            // include FRB_XNA in them

            ProjectBase toReturn = null;

            if (preProcessorConstants.Contains("ANDROID"))
            {
                toReturn = new AndroidProject(coreVisualStudioProject);
            }
            else if (preProcessorConstants.Contains("WINDOWS_8"))
            {
                toReturn = new Windows8MonoGameProject(coreVisualStudioProject);
            }
            else if(preProcessorConstants.Contains("IOS"))
            {
                toReturn = new IosMonogameProject(coreVisualStudioProject);
            }
            else if(preProcessorConstants.Contains("UWP"))
            {
                toReturn = new UwpProject(coreVisualStudioProject);
            }
            else if (preProcessorConstants.Contains("WINDOWS_PHONE"))
            {
                toReturn = new WindowsPhoneProject(coreVisualStudioProject);
            }

            else if (preProcessorConstants.Contains("XNA4"))
            {
                toReturn = new Xna4Project(coreVisualStudioProject);
            }

            else if (preProcessorConstants.Contains("FRB_XNA"))
            {
                toReturn = new XnaProject(coreVisualStudioProject);
            }

            else if (preProcessorConstants.Contains("FSB") || preProcessorConstants.Contains("SILVERLIGHT"))
            {
                toReturn = new FsbProject(coreVisualStudioProject);
            }

            else if (preProcessorConstants.Contains("FRB_MDX"))
            {
                toReturn = new MdxProject(coreVisualStudioProject);
            }
            
            return toReturn;
        }

        public static string GetPreProcessorConstantsFromProject(Project coreVisualStudioProject)
        {
            string preProcessorConstants = "";

            // Victor Chelaru October 20, 2012
            // We used to just look at the XML and had a broad way of determining the 
            // patterns.  I decided it was time to clean this up and make it more precise
            // so now we use the PropertyGroups from the project.
            foreach (var property in coreVisualStudioProject.Properties)
            {
                if (property.Name == "DefineConstants")
                {
                    preProcessorConstants += ";" + property.EvaluatedValue;
                }
            }
            return preProcessorConstants;
        }

    }
}
