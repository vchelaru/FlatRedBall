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
    [Export(typeof(PluginBase))]
    public class FrbSourcePlugin : PluginBase
    {
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
            fbd.Description = "Select FlatRedBall Source Folder";
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
            fbd.Description = "Select Gum Source Folder";
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

            //ToDo Handle output and errors
            if (!AddProject(referencedProject, sln.FullFileName, $"{frbSourceFolder}\\Engines\\Forms\\FlatRedBall.Forms\\StateInterpolation\\StateInterpolation.DesktopGL.csproj"))
                return;

            if (!AddProject(referencedProject, sln.FullFileName, $"{frbSourceFolder}\\Engines\\FlatRedBallXNA\\FlatRedBall\\FlatRedBallDesktopGL.csproj"))
                return;

            if (!AddProject(referencedProject, sln.FullFileName, $"{frbSourceFolder}\\Engines\\Forms\\FlatRedBall.Forms\\FlatRedBall.Forms\\FlatRedBall.Forms.DesktopGL.csproj"))
                return;

            if (!AddProject(referencedProject, sln.FullFileName, $"{gumSourceFolder}\\GumCore\\GumCoreXnaPc\\GumCoreDesktopGL.csproj"))
                return;

            if (!AddSharedProject(referencedProject, sln.FullFileName, $"{frbSourceFolder}\\Engines\\FlatREdBallXNA\\FlatRedBall\\FlatREdBallShared.shproj", Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"), Guid.Parse("0BB8CBE3-8503-46C1-9272-D98E153A230E"), "FlatRedBallShared", new List<VSSolution.SharedProject> { }, new List<string> { }, new List<string> { }))
                return;

            if (!AddSharedProject(referencedProject, sln.FullFileName, $"{frbSourceFolder}\\Engines\\Forms\\FlatRedBall.Forms\\FlatREdBall.Forms.Shared\\FlatRedBall.Forms.Shared.shproj", Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"), Guid.Parse("728151F0-03E0-4253-94FE-46B9C77EDEA6"), "FlatRedBall.Forms.Shared", new List<VSSolution.SharedProject> { }, new List<string> { }, new List<string> { }))
                return;

            if (!AddSharedProject(referencedProject, sln.FullFileName, $"{gumSourceFolder}\\GumCoreShared.shproj", Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"), Guid.Parse("F919C045-EAC7-4806-9A1F-CE421F923E97"), "GumCoreShared", new List<VSSolution.SharedProject> { }, new List<string> { }, new List<string> { }))
                return;

            var proj = GlueState.Self.CurrentMainProject;

            RemoveReference(proj, "FlatRedBall.Forms");
            RemoveReference(proj, "FlatRedBallDesktopGL");
            RemoveReference(proj, "GumCoreXnaPc");
            RemoveReference(proj, "StateInterpolation");

            if(!proj.HasProjectReference("StateInterpolation.DesktopGL"))
                proj.AddProjectReference($"{frbSourceFolder}\\Engines\\Forms\\FlatRedBall.Forms\\StateInterpolation\\StateInterpolation.DesktopGL.csproj");

            if (!proj.HasProjectReference("FlatRedBallDesktopGL"))
                proj.AddProjectReference($"{frbSourceFolder}\\Engines\\FlatRedBallXNA\\FlatRedBall\\FlatRedBallDesktopGL.csproj");

            if (!proj.HasProjectReference("FlatRedBall.Forms.DesktopGL"))
                proj.AddProjectReference($"{frbSourceFolder}\\Engines\\Forms\\FlatRedBall.Forms\\FlatRedBall.Forms\\FlatRedBall.Forms.DesktopGL.csproj");

            if (!proj.HasProjectReference("GumCoreDesktopGL"))
                proj.AddProjectReference($"{gumSourceFolder}\\GumCore\\GumCoreXnaPc\\GumCoreDesktopGL.csproj");

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

        private bool AddSharedProject(List<string> existingProjects, FilePath solution, FilePath project, Guid projectTypeId, Guid projectId, string projectName, List<VSSolution.SharedProject> sharedProjects, List<string> projectConfigurations, List<string> solutionConfigurations)
        {
            var relativePath = project.RelativeTo(solution.GetDirectoryContainingThis());

            if (existingProjects.Any(item => item.ToLower() == relativePath.ToLower()))
                return true;

            if (!VSSolution.AddExistingProject(solution, projectTypeId, projectId, projectName, project, sharedProjects, projectConfigurations, solutionConfigurations, out var errorMessages))
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

            if (!CheckFileExists($"{path}\\Engines\\Forms\\FlatRedBall.Forms\\StateInterpolation\\StateInterpolation.DesktopGL.csproj", out error))
                return false;

            if (!CheckFileExists($"{path}\\Engines\\FlatRedBallXNA\\FlatRedBall\\FlatRedBallDesktopGL.csproj", out error))
                return false;

            if (!CheckFileExists($"{path}\\Engines\\FlatREdBallXNA\\FlatRedBall\\FlatREdBallShared.shproj", out error))
                return false;

            if (!CheckFileExists($"{path}\\Engines\\Forms\\FlatRedBall.Forms\\FlatRedBall.Forms\\FlatRedBall.Forms.DesktopGL.csproj", out error))
                return false;

            if (!CheckFileExists($"{path}\\Engines\\Forms\\FlatRedBall.Forms\\FlatREdBall.Forms.Shared\\FlatRedBall.Forms.Shared.shproj", out error))
                return false;

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

            if (!CheckFileExists($"{path}\\GumCore\\GumCoreXnaPc\\GumCoreDesktopGL.csproj", out error))
                return false;

            if (!CheckFileExists($"{path}\\GumCoreShared.shproj", out error))
                return false;

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
