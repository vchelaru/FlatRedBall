using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using Microsoft.VisualBasic.ApplicationServices;
using OfficialPlugins.FrbSourcePlugin.ViewModels;
using PluginTestbed.GlobalContentManagerPlugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GeneralResponse = ToolsUtilities.GeneralResponse;


namespace OfficialPlugins.FrbSourcePlugin.Managers;

internal static class AddSourceManager
{
    static string GithubFilePath =>
    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GitHub");

    public static string DefaultFrbFilePath =>
        System.IO.Path.Combine(GithubFilePath, "FlatRedBall");

    public static string DefaultGumFilePath =>
        System.IO.Path.Combine(GithubFilePath, "Gum");

    #region DesktopGlNetFramework Projects

    public static List<ProjectReference> SharedShprojReferences = new List<ProjectReference>
    {
        new ProjectReference()
        {
            RelativeProjectFilePath = $"Engines\\FlatRedBallXNA\\FlatRedBall\\FlatRedBallShared.shproj",
            ProjectTypeId = Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"),
            ProjectId = Guid.Parse("0BB8CBE3-8503-46C1-9272-D98E153A230E"),
            ProjectRootType = FrbOrGum.Frb
        },

        new ProjectReference()
        {
            RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\FlatRedBall.Forms.Shared\\FlatRedBall.Forms.Shared.shproj",
            ProjectTypeId = Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"),
            ProjectId = Guid.Parse("728151F0-03E0-4253-94FE-46B9C77EDEA6"),
            ProjectRootType = FrbOrGum.Frb
        },

        new ProjectReference()
        {
            RelativeProjectFilePath = $"GumCoreShared.shproj",
            ProjectTypeId = Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"),
            ProjectId = Guid.Parse("F919C045-EAC7-4806-9A1F-CE421F923E97"),
            ProjectRootType = FrbOrGum.Gum

        },

        new ProjectReference()
        {
            RelativeProjectFilePath = $"FRBDK\\Glue\\GumPlugin\\GumPlugin\\GumCoreShared.FlatRedBall.shproj",
            ProjectTypeId = Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"),
            ProjectId = Guid.Parse("0ee8a96c-a754-453d-9e65-19a24e9a5e76"),
            ProjectRootType = FrbOrGum.Frb
        },

        new ProjectReference()
        {
            RelativeProjectFilePath = $"FRBDK\\Glue\\StateInterpolationPlugin\\StateInterpolationPlugin\\StateInterpolationShared.shproj",
            ProjectTypeId = Guid.Parse("D954291E-2A0B-460D-934E-DC6B0785DB48"),
            ProjectId = Guid.Parse("d00d287d-385b-42fb-bf5f-04401e7d37d0"),
            ProjectRootType = FrbOrGum.Frb
        },

    };

    public static List<ProjectReference> DesktopGlNetFramework = new List<ProjectReference>
    {
        new ProjectReference(){ RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\StateInterpolation\\StateInterpolation.DesktopGL.csproj", ProjectRootType = FrbOrGum.Frb},
        new ProjectReference(){ RelativeProjectFilePath = $"Engines\\FlatRedBallXNA\\FlatRedBall\\FlatRedBallDesktopGL.csproj", ProjectRootType = FrbOrGum.Frb},
        new ProjectReference(){ RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\FlatRedBall.Forms\\FlatRedBall.Forms.DesktopGL.csproj", ProjectRootType = FrbOrGum.Frb},
        new ProjectReference(){ RelativeProjectFilePath = $"GumCore\\GumCoreXnaPc\\GumCoreDesktopGL.csproj", ProjectRootType = FrbOrGum.Gum},

    };

    public static List<ProjectReference> AndroidXamarin = new List<ProjectReference>
    {
        new ProjectReference(){ RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\StateInterpolation\\StateInterpolation.Android.csproj", ProjectRootType = FrbOrGum.Frb},
        new ProjectReference(){ RelativeProjectFilePath = $"Engines\\FlatRedBallXNA\\FlatRedBall\\FlatRedBallAndroidv2.csproj", ProjectRootType = FrbOrGum.Frb},
        new ProjectReference(){ RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\FlatRedBall.Forms\\FlatRedBall.Forms.Android.csproj", ProjectRootType = FrbOrGum.Frb},
        new ProjectReference(){ RelativeProjectFilePath = $"GumCore\\GumCoreXnaPc\\GumCoreAndroid.csproj", ProjectRootType = FrbOrGum.Gum},
    };

    #endregion

    #region DesktopGlNet6 Projects

    public static List<ProjectReference> DesktopGlNet6 = new List<ProjectReference>
    {
        new ProjectReference(){ RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\StateInterpolation\\StateInterpolation.DesktopGlNet6\\StateInterpolation.DesktopNet6.csproj", ProjectRootType = FrbOrGum.Frb},
        new ProjectReference(){ RelativeProjectFilePath = $"Engines\\FlatRedBallXNA\\FlatRedBallDesktopGLNet6\\FlatRedBallDesktopGLNet6.csproj", ProjectRootType = FrbOrGum.Frb},
        new ProjectReference(){ RelativeProjectFilePath = $"Engines\\Forms\\FlatRedBall.Forms\\FlatRedBall.Forms.DesktopGlNet6\\FlatRedBall.Forms.DesktopGlNet6.csproj", ProjectRootType = FrbOrGum.Frb},
        new ProjectReference(){ RelativeProjectFilePath = $"GumCore\\GumCoreXnaPc\\GumCore.DesktopGlNet6\\GumCore.DesktopGlNet6.csproj", ProjectRootType = FrbOrGum.Gum},
    };

    static ProjectReference GumSkia = new ProjectReference
    {
        RelativeProjectFilePath = $"SvgPlugin\\SkiaInGumShared\\SkiaInGum.csproj",
        ProjectRootType = FrbOrGum.Gum
    };

    #endregion

    public static async Task HandleLinkToSourceClicked(AddFrbSourceViewModel viewModel)
    {
        var frbRootFolder = viewModel.FrbRootFolder;
        var gumRootFolder = viewModel.GumRootFolder;
        bool includeGumSkia = viewModel.IncludeGumSkia;
        await LinkToSourceInternal(frbRootFolder, gumRootFolder, includeGumSkia);
    }

    public static async Task LinkToSourceUsingDefaults()
    {
        var frbRootFolder = DefaultFrbFilePath;
        var gumRootFolder = DefaultGumFilePath;
        bool includeGumSkia = true;
        await LinkToSourceInternal(frbRootFolder, gumRootFolder, includeGumSkia);
    }

    private static async Task LinkToSourceInternal(string frbRootFolder, string gumRootFolder, bool includeGumSkia)
    {
        await TaskManager.Self.AddAsync(() =>
        {
            var projectReferences = GetProjectReferencesForCurrentProject();

            string outerError = null;
            string innerError;

            if (!ValidateSourceFRB(frbRootFolder, projectReferences, out innerError))
            {
                outerError = $"Selected FlatRedBall path is not valid.  Error: {innerError}";
            }

            if (!ValidateSourceGum(gumRootFolder, projectReferences, out innerError))
            {
                outerError = $"Selected Gum path is not valid.  Error: {innerError}";
            }

            if (includeGumSkia)
            {
                if (GlueState.Self.CurrentMainProject.DotNetVersion.Major >= 6 == false)
                {
                    outerError = "GumSkia can only be added to .NET 6 and greater projects";
                }
                else if (GlueState.Self.CurrentGlueProject.FileVersion < (int)GlueProjectSave.GluxVersions.HasGumSkiaElements)
                {
                    outerError = $"GumSkia can only be added to FlatRedBall projects (.gluj) which are version {(int)GlueProjectSave.GluxVersions.HasGumSkiaElements} or newer";
                }
            }

            if (!string.IsNullOrEmpty(outerError))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(outerError);
                return;
            }


            if (!string.IsNullOrEmpty(frbRootFolder) && !string.IsNullOrEmpty(gumRootFolder))
            {


                //Update project references
                var sln = VSSolution.FromFile(GlueState.Self.CurrentSlnFileName);

                var referencedProject = sln.ReferencedProjects;

                var proj = GlueState.Self.CurrentMainProject;

                GeneralResponse addGeneralResponse = GeneralResponse.SuccessfulResponse;

                foreach (var projectReference in projectReferences)
                {
                    AddProjectReference(sln, referencedProject, proj, addGeneralResponse, projectReference, frbRootFolder, gumRootFolder);
                }

                if (includeGumSkia)
                {
                    AddProjectReference(sln, referencedProject, proj, addGeneralResponse, GumSkia, frbRootFolder, gumRootFolder);
                }

                if (addGeneralResponse.Succeeded)
                {
                    RemoveDllReference(proj, "FlatRedBall.Forms");
                    RemoveDllReference(proj, "FlatRedBallDesktopGL");
                    RemoveDllReference(proj, "FlatRedBallAndroid");
                    RemoveDllReference(proj, "GumCoreXnaPc");
                    RemoveDllReference(proj, "GumCoreAndroid");
                    RemoveDllReference(proj, "GumCore.DesktopGlNet6");
                    RemoveDllReference(proj, "StateInterpolation");
                    RemoveDllReference(proj, "SkiaInGum");

                    RemoveNugetReference(proj, "FlatRedBallDesktopGLNet6");

                    proj.Save(proj.FullFileName.FullPath);
                }
            }

        }, "Linking game to FRB Source");
    }

    private static List<ProjectReference> GetProjectReferencesForCurrentProject()
    {
        if (GlueState.Self.CurrentMainProject.DotNetVersion.Major >= 6)
        {
            // When we support Android/iOS .NET 6, we need to handle those here:
            return DesktopGlNet6.Concat(SharedShprojReferences).ToList();
        }
        else if(GlueState.Self.CurrentMainProject is AndroidProject)
        {
            return AndroidXamarin.Concat(SharedShprojReferences).ToList();
        }
        else
        {
            return DesktopGlNetFramework.Concat(SharedShprojReferences).ToList();
        }
    }

    private static bool ValidateSourceFRB(string path, List<ProjectReference> projectReferences, out string error)
    {
        if (!Directory.Exists(path))
        {
            error = "Path is not a directory that exists";
            return false;
        }

        foreach (var project in projectReferences.Where(item => item.ProjectRootType == FrbOrGum.Frb))
        {
            if (!CheckFileExists($"{path}\\{project.RelativeProjectFilePath}", out error))
                return false;
        }

        error = null;
        return true;
    }

    private static bool CheckFileExists(string path, out string error)
    {
        if (!File.Exists(path))
        {
            error = $"{path} does not exist.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateSourceGum(string path, List<ProjectReference> projectReferences, out string error)
    {
        if (!Directory.Exists(path))
        {
            error = "Path is not a directory that exists";
            return false;
        }

        foreach (var project in projectReferences.Where(item => item.ProjectRootType == FrbOrGum.Gum))
        {

            if (!CheckFileExists($"{path}\\{project.RelativeProjectFilePath}", out error))
                return false;
        }

        error = null;
        return true;
    }

    private static void AddProjectReference(VSSolution sln, List<string> referencedProject, VisualStudioProject proj, 
        GeneralResponse addGeneralResponse, ProjectReference reference,
        string frbRootFolder, string gumRootFolder
        )
    {
        var prefix = reference.ProjectRootType == FrbOrGum.Frb
                                ? frbRootFolder + "\\"
                                : gumRootFolder + "\\";

        FilePath fullPath = prefix + reference.RelativeProjectFilePath;

        var extension = fullPath.Extension;

        if (extension == "csproj")
        {
            if (!AddProject(referencedProject, sln.FullFileName, fullPath))
            {
                addGeneralResponse.Succeeded = false;
                addGeneralResponse.Message = $"Failed to add {fullPath}";
            }


        }
        else if (extension == "shproj")
        {
            if (!AddSharedProject(referencedProject, sln.FullFileName, fullPath, reference.ProjectTypeId, reference.ProjectId, fullPath.NoPathNoExtension))
            {
                addGeneralResponse.Succeeded = false;
                addGeneralResponse.Message = $"Failed to add {fullPath}";
            }
        }

        if (addGeneralResponse.Succeeded && !proj.HasProjectReference(fullPath.NoPathNoExtension) && fullPath.Extension == "csproj")
        {
            proj.AddProjectReference(fullPath.FullPath);
        }
    }

    private static void RemoveDllReference(VisualStudioProject project, string referenceName)
    {
        if (project.EvaluatedItems.Any(item => item.ItemType == "Reference" && item.EvaluatedInclude.StartsWith(referenceName)))
        {
            var item = project.EvaluatedItems.Where(item => item.ItemType == "Reference" && item.EvaluatedInclude.StartsWith(referenceName)).First();

            project.RemoveItem(item);
        }
    }

    private static void RemoveNugetReference(VisualStudioProject project, string referenceName)
    {
        var item = project.EvaluatedItems.FirstOrDefault(item => item.ItemType == "PackageReference" && item.EvaluatedInclude == referenceName);

        if(item != null) 
        {
            project.RemoveItem(item);
        }
    }



    private static bool AddProject(List<string> existingProjects, FilePath solution, FilePath project)
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

    private static bool AddSharedProject(List<string> existingProjects, FilePath solution, FilePath project, Guid projectTypeId, Guid projectId, string projectName)
    {
        var relativePath = project.RelativeTo(solution.GetDirectoryContainingThis());

        var standardized = FileManager.Standardize(relativePath, null, false).ToLowerInvariant();

        var existingStandardized =
            existingProjects.Select(item => FileManager.Standardize(item, null, false).ToLowerInvariant()).ToArray();

        if (existingStandardized.Any(item => item == standardized))
            return true;

        if (!VSSolution.AddExistingProject(solution, projectTypeId, projectId, projectName, project, new List<VSSolution.SharedProject> { }, new List<string>(), new List<string>(), out var errorMessages))
        {
            MessageBox.Show($"Failed to add project {project}. Errors: {errorMessages}");
            return false;
        }

        return true;
    }

}
