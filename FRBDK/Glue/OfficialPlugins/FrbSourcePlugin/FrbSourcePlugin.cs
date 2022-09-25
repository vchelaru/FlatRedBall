using System;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using System.IO;
using System.Collections.Generic;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
using System.Linq;

namespace PluginTestbed.GlobalContentManagerPlugins
{
    public enum FrbOrGum
    {
        Frb,
        Gum
    }

    public class ProjectReference
    {
        public FrbOrGum FrbOrGum;
        public string RelativeProjectFilePath;
        public Guid ProjectTypeId;
        public Guid ProjectId;
        public string ProjectName;
        public List<VSSolution.SharedProject> SharedProjects;
        public List<string> ProjectConfigurations;
        public List<string> SolutionConfigurations;
    }


    [Export(typeof(PluginBase))]
    public class FrbSourcePlugin : PluginBase
    {
        public List<ProjectReference> DesktopGlNetFramework = new List<ProjectReference>
        {
            new ProjectReference(){ RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\StateInterpolation\\StateInterpolation.DesktopGL.csproj", FrbOrGum = FrbOrGum.Frb},
            new ProjectReference(){ RelativeProjectFilePath = $"Engines\\FlatRedBallXNA\\FlatRedBall\\FlatRedBallDesktopGL.csproj", FrbOrGum = FrbOrGum.Frb},
            new ProjectReference(){ RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\FlatRedBall.Forms\\FlatRedBall.Forms.DesktopGL.csproj", FrbOrGum = FrbOrGum.Frb},
            new ProjectReference(){ RelativeProjectFilePath = $"GumCore\\GumCoreXnaPc\\GumCoreDesktopGL.csproj", FrbOrGum = FrbOrGum.Gum},

            new ProjectReference()
            { 
                RelativeProjectFilePath = $"Engines\\FlatRedBallXNA\\FlatRedBall\\FlatRedBallShared.shproj", 
                ProjectTypeId = Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"), 
                ProjectId = Guid.Parse("0BB8CBE3-8503-46C1-9272-D98E153A230E"),
                FrbOrGum = FrbOrGum.Frb
            },

            new ProjectReference()
            {
                RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\FlatRedBall.Forms.Shared\\FlatRedBall.Forms.Shared.shproj",
                ProjectTypeId = Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"),
                ProjectId = Guid.Parse("728151F0-03E0-4253-94FE-46B9C77EDEA6"),
                FrbOrGum = FrbOrGum.Frb
            },

            new ProjectReference()
            {
                RelativeProjectFilePath = $"GumCoreShared.shproj",
                ProjectTypeId = Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"),
                ProjectId = Guid.Parse("F919C045-EAC7-4806-9A1F-CE421F923E97"),
                FrbOrGum = FrbOrGum.Gum

            },

            // finish here....
        };

        private ToolStripMenuItem miLinkSource;

        public override string FriendlyName => "FRB Source";

        public override Version Version => new Version(1, 0);



        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            miLinkSource.Owner.Items.Remove(miLinkSource);

            this.ReactToLoadedGlux -= HandleGluxLoaded;
            this.ReactToUnloadedGlux -= HandleGluxUnloaded;

            return true;
        }

        public override void StartUp()
        {
            miLinkSource = this.AddMenuItemTo("Link Game to FRB Source", (not, used) => LinkGameToGlueSource(), "Update");

            miLinkSource.Enabled = false;

            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;
        }

        private void HandleGluxUnloaded()
        {
            miLinkSource.Enabled = false;
        }

        private void HandleGluxLoaded()
        {
            if(GlueState.Self.CurrentMainProject is DesktopGlProject)
            {
                miLinkSource.Enabled = true;
            }
        }

        private void LinkGameToGlueSource()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.UseDescriptionForTitle = true;

            //Get FRB Source Folder and Validate
            fbd.Description = "Select FlatRedBall Repository Root";
            System.Windows.Forms.DialogResult result = fbd.ShowDialog();

            string frbSourceFolder;
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                frbSourceFolder = fbd.SelectedPath;

                if(!ValidateSourceFRB(frbSourceFolder, out var error))
                {
                    MessageBox.Show($"Selected source path is not valid.  Error: {error}", "Source Not Valid", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                return;
            }

            //Get Gum Source Folder and Validate
            fbd.Description = "Select Gum Repository Root";
            result = fbd.ShowDialog();

            string gumSourceFolder;
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                gumSourceFolder = fbd.SelectedPath;

                if (!ValidateSourceGum(gumSourceFolder, out var error))
                {
                    MessageBox.Show($"Selected source path is not valid.  Error: {error}", "Source Not Valid", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                return;
            }

            //Update project references
            var sln = VSSolution.FromFile(GlueState.Self.CurrentSlnFileName);

            var referencedProject = sln.ReferencedProjects;

            var proj = GlueState.Self.CurrentMainProject;

            foreach(var reference in DesktopGlNetFramework)
            {
                var prefix = reference.FrbOrGum == FrbOrGum.Frb
                    ? frbSourceFolder + "\\"
                    : gumSourceFolder + "\\";

                FilePath fullPath = prefix + reference.RelativeProjectFilePath;

                var extension = fullPath.Extension;

                if(extension == "csproj")
                {
                    if (!AddProject(referencedProject, sln.FullFileName, fullPath))
                        return;
                }
                else if(extension == "shproj")
                {
                    if (!AddSharedProject(referencedProject, sln.FullFileName, fullPath, reference.ProjectTypeId, reference.ProjectId, fullPath.NoPathNoExtension))
                    {
                        //ToDo Handle output and errors
                        return;
                    }
                }

                if(!proj.HasProjectReference(fullPath.NoPathNoExtension) && fullPath.Extension == "csproj")
                {
                    proj.AddProjectReference(fullPath.FullPath);

                }
            }

            RemoveReference(proj, "FlatRedBall.Forms");
            RemoveReference(proj, "FlatRedBallDesktopGL");
            RemoveReference(proj, "GumCoreXnaPc");
            RemoveReference(proj, "StateInterpolation");

            proj.Save(proj.FullFileName.FullPath);
        }

        private void RemoveReference(VisualStudioProject project, string referenceName)
        {
            if (project.EvaluatedItems.Any(item => item.ItemType == "Reference" && item.EvaluatedInclude.StartsWith(referenceName)))
            {
                var item = project.EvaluatedItems.Where(item => item.ItemType == "Reference" && item.EvaluatedInclude.StartsWith(referenceName)).First();

                project.RemoveItem(item);
            }
        }

        private bool AddProject(List<string> existingProjects, FilePath solution, FilePath project)
        {
            var relativePath = new FilePath(project.RelativeTo(solution.GetDirectoryContainingThis()));

            if (existingProjects.Any(item => new FilePath(item) == relativePath))
                return true;

            if (!VSSolution.AddExistingProjectWithDotNet(solution, project, out var outputMessages, out var errorMessages))
            {
                MessageBox.Show($"Failed to add project {project}. Errors: {errorMessages}");
                return false;
            }

            return true;
        }

        private bool AddSharedProject(List<string> existingProjects, FilePath solution, FilePath project, Guid projectTypeId, Guid projectId, string projectName)
        {
            var relativePath = project.RelativeTo(solution.GetDirectoryContainingThis());

            if (existingProjects.Any(item => item.ToLower() == relativePath.ToLower()))
                return true;

            if (!VSSolution.AddExistingProject(solution, projectTypeId, projectId, projectName, project, new List<VSSolution.SharedProject> { }, new List<string>(), new List<string>(), out var errorMessages))
            {
                MessageBox.Show($"Failed to add project {project}. Errors: {errorMessages}");
                return false;
            }

            return true;
        }

        private bool ValidateSourceFRB(string path, out string error)
        {
            if (!Directory.Exists(path))
            {
                error = "Path is not a directory that exists";
                return false;
            }

            foreach(var project in DesktopGlNetFramework.Where(item => item.FrbOrGum == FrbOrGum.Frb))
            {
                if (!CheckFileExists($"{path}\\{project.RelativeProjectFilePath}", out error))
                    return false;
            }

            error = null;
            return true;
        }

        private bool ValidateSourceGum(string path, out string error)
        {
            if (!Directory.Exists(path))
            {
                error = "Path is not a directory that exists";
                return false;
            }

            foreach(var project in DesktopGlNetFramework.Where(item => item.FrbOrGum == FrbOrGum.Gum))
            {

                if (!CheckFileExists($"{path}\\{project.RelativeProjectFilePath}", out error))
                    return false;
            }

            error = null;
            return true;
        }

        private bool CheckFileExists(string path, out string error)
        {
            if (!File.Exists(path))
            {
                error = $"{path} does not exist.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
