using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using Glue;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel.Composition;
using System.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.VSHelpers
{
    [Export(typeof(PluginBase))]
    public class ProjectSyncer : PluginBase
    {
        static ProjectSyncer mSelf;

        public static ProjectSyncer Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ProjectSyncer();
                }
                return mSelf;
            }
        }

        public static void UpdateSyncedProjectsInGlux()
        {
            // See if there are any new projects

            var glueProject = GlueState.Self.CurrentGlueProject;

            if (glueProject != null)
            {
                foreach (ProjectBase vsp in ProjectManager.SyncedProjects)
                {
                    string fileNameToUse;

                    if (!vsp.SaveAsAbsoluteSyncedProject)
                    {
                        fileNameToUse = FileManager.MakeRelative(vsp.FullFileName.FullPath);
                    }
                    else
                    {
                        fileNameToUse = vsp.FullFileName.FullPath;
                    }

                    if (!glueProject.SyncedProjects.Contains(fileNameToUse))
                    {
                        glueProject.SyncedProjects.Add(fileNameToUse);
                    }
                }


                for (int i = glueProject.SyncedProjects.Count - 1; i > -1; i--)
                {
                    string projectName = glueProject.SyncedProjects[i];

                    // is the project part of what's in the ProjectManager?

                    bool isPartOfProject = false;

                    foreach (ProjectBase vsp in ProjectManager.SyncedProjects)
                    {
                        if (!vsp.SaveAsAbsoluteSyncedProject && FileManager.MakeRelative(vsp.FullFileName.FullPath) == projectName)
                        {
                            isPartOfProject = true;
                            break;
                        }
                        else if (vsp.SaveAsAbsoluteSyncedProject && vsp.FullFileName == projectName)
                        {
                            isPartOfProject = true;
                            break;
                        }
                    }

                    if (!isPartOfProject)
                    {
                        glueProject.SyncedProjects.RemoveAt(i);
                    }

                }
            }
        }


        public static void SyncProjects(ProjectBase sourceProjectBase, ProjectBase projectBaseToModify, bool performTranslation)
        {
            projectBaseToModify.SyncTo(sourceProjectBase, performTranslation);
        }

        
        public static string LocateSolution(string projectFileName)
        {
            List<String> dirFileList = null;
            List<String> parentDirFileList = null;
            string directory = FileManager.GetDirectory(projectFileName);
            string baseFile = FileManager.RemovePath(FileManager.RemoveExtension(projectFileName));
            string solutionFileName = "";

            #region Search directory for "filename".sln
            dirFileList = FileManager.GetAllFilesInDirectory(directory, "sln", 0);

            if (dirFileList.Count != 0)
            {
                foreach (string file in dirFileList)
                {
                    if (FileManager.RemovePath(file).StartsWith(baseFile))
                    {
                        solutionFileName = file;
                        break;
                    }
                }
            }
            #endregion

            #region Search one directory above for "filename".sln
            if (string.IsNullOrEmpty(solutionFileName))
            {
                parentDirFileList = FileManager.GetAllFilesInDirectory(FileManager.GetDirectory(directory), "sln", 0);

                if (parentDirFileList.Count != 0)
                {
                    foreach (string file in parentDirFileList)
                    {
                        if (FileManager.RemovePath(file).StartsWith(baseFile))
                        {
                            solutionFileName = file;
                            break;
                        }
                    }
                }
            }
            #endregion

            #region Search same directory for *.sln

            if (string.IsNullOrEmpty(solutionFileName) && dirFileList.Count != 0)
                solutionFileName = dirFileList[0];
            #endregion

            #region Search parent directory for *.sln
            if (string.IsNullOrEmpty(solutionFileName) && parentDirFileList.Count != 0)
                solutionFileName = parentDirFileList[0];
            #endregion

            #region This MUST be an FSB project, but search another directory up for sln
            if (string.IsNullOrEmpty(solutionFileName))
            {
                dirFileList = FileManager.GetAllFilesInDirectory(FileManager.GetDirectory(FileManager.GetDirectory(directory)),
                                                                 "sln", 0);
                if (dirFileList.Count != 0)
                    solutionFileName = dirFileList[0];
            }
            #endregion




            if (string.IsNullOrEmpty(solutionFileName))
            {
                throw new FileNotFoundException("Could not find a .sln file for the argument project");
            }
            else
            {
                // open it to make sure it actually references the .csproj, because it may not...
                var contents = System.IO.File.ReadAllText(solutionFileName);

                var whatToSearchFor = (baseFile + ".csproj").ToLowerInvariant();

                bool doesReferenceProject = contents.ToLowerInvariant().Contains(whatToSearchFor);

                if(!doesReferenceProject)
                {
                    throw new FileNotFoundException($"Found the .sln file \"{solutionFileName}\" but it does not reference the project \"{baseFile}.csproj\"");
                }
            }


            return solutionFileName;

        }

        static void OpenSyncedProject(object sender, EventArgs e)
        {
            ProjectBase projectBase = (ProjectBase)((ToolStripItem)sender).Tag;

            string solutionName = LocateSolution(projectBase.FullFileName.FullPath);

            if (!string.IsNullOrEmpty(solutionName))
            {
                if (!PluginManager.OpenSolution(solutionName))
                {
                    Process.Start(solutionName);
                }
            }
            else
            {
                if (!PluginManager.OpenProject(projectBase.FullFileName.FullPath))
                {
                    Process.Start(projectBase.FullFileName.FullPath);
                }
            }
        }

        public override string FriendlyName
        {
            get { return "Project Syncer"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }

        public override void StartUp()
        {
        }
        

        public override bool ShutDown(Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            return false;
        }
    }
}
