using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace OfficialPlugins.FrbSourcePlugin.Managers;

/// <summary>
/// Links an FRB2 (FlatRedBall 2) game project to a sibling FlatRedBall2 source checkout. Deliberately
/// separate from <see cref="AddSourceManager"/> - that class's shared/platform-project lists and DLL
/// cleanup are FRB1-only (.shproj shared projects, a classic .sln) and don't apply here: an FRB2
/// template project is a single engine PackageReference on a plain .slnx solution.
/// </summary>
/// <remarks>
/// Desktop-only for now (the <c>frb2-desktop</c> template's Common+Desktop shape, one TFM, one
/// PackageReference). <c>frb2-multiplatform</c> multi-targets Common across net8/net10 with a
/// per-TFM engine package (KNI vs MonoGame) and needs its own conditional ProjectReference handling.
/// </remarks>
internal class Frb2AddSourceManager
{
    /// <summary>Test seam: overrides the "Documents/GitHub" root the default lookup uses below.</summary>
    internal static string GithubFilePathOverrideForTesting { get; set; }

    string GithubFilePath => GithubFilePathOverrideForTesting ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GitHub");

    public string DefaultFrb2EngineCsprojPath =>
        Path.Combine(GithubFilePath, "FlatRedBall2", "src", "FlatRedBall2.csproj");

    public bool HasFrb2RepoInDefaultLocation() => File.Exists(DefaultFrb2EngineCsprojPath);

    public async Task LinkToSourceUsingDefaults(Frb2Project frb2Project) =>
        await LinkToSource(frb2Project, DefaultFrb2EngineCsprojPath);

    public async Task LinkToSource(Frb2Project frb2Project, string engineCsprojFullPath)
    {
        await TaskManager.Self.AddAsync(() =>
        {
            var response = LinkFrb2ProjectToSource(frb2Project, engineCsprojFullPath);

            if (!response.Succeeded)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(response.Message);
            }
        }, "Linking game to FlatRedBall2 Source");
    }

    /// <summary>
    /// The testable core: given an explicit engine .csproj path, swaps the project's
    /// FlatRedBall2.* PackageReference for a ProjectReference to it, and adds the engine project to
    /// the game's solution (.slnx-safe, via `dotnet sln add`).
    /// </summary>
    internal GeneralResponse LinkFrb2ProjectToSource(Frb2Project frb2Project, string engineCsprojFullPath)
    {
        if (!File.Exists(engineCsprojFullPath))
        {
            return GeneralResponse.UnsuccessfulWith(
                $"Could not find FlatRedBall2 source at {engineCsprojFullPath}.");
        }

        var engineProjectNameNoExtension = FileManager.RemoveExtension(FileManager.RemovePath(engineCsprojFullPath));

        var packageReferences = frb2Project.EvaluatedItems
            .Where(item => item.ItemType == "PackageReference" &&
                FlatRedBall.Glue.VSHelpers.Projects.Frb2ProjectDetector.IsFrb2Package(item.EvaluatedInclude))
            .ToList();

        var alreadyReferencedAsSource = frb2Project.HasProjectReference(engineProjectNameNoExtension);

        if (packageReferences.Count == 0 && alreadyReferencedAsSource)
        {
            // Already linked - nothing to do.
            return GeneralResponse.SuccessfulResponse;
        }

        var slnFilePath = GlueState.Self.SlnFileForProject(frb2Project);
        var existingSln = VSSolution.FromFile(slnFilePath);
        var alreadyInSln = existingSln.ReferencedProjects.Any(reference =>
            string.Equals(FileManager.RemovePath(reference.Name), FileManager.RemovePath(engineCsprojFullPath),
                StringComparison.OrdinalIgnoreCase));

        if (!alreadyInSln)
        {
            if (!VSSolution.AddExistingProjectWithDotNet(slnFilePath, new FilePath(engineCsprojFullPath), out _, out var slnError))
            {
                return GeneralResponse.UnsuccessfulWith(
                    $"Failed to add {engineCsprojFullPath} to the solution. Errors: {slnError}");
            }
        }

        foreach (var packageReference in packageReferences)
        {
            frb2Project.RemoveItem(packageReference);
        }

        if (!alreadyReferencedAsSource)
        {
            frb2Project.AddProjectReference(engineCsprojFullPath);
        }

        frb2Project.SaveEvenIfNotMaintainedByGlue(frb2Project.FullFileName.FullPath);

        return GeneralResponse.SuccessfulResponse;
    }
}
