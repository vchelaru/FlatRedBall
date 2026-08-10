using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GlueUnitTests.TestSupport;
using Npc;
using Npc.Data;
using Shouldly;

namespace GlueUnitTests.Projects;

// The New Project wizard's platform list (Npc.Data.EmptyTemplates) and the release tooling's engine
// list (BuildServerUploaderConsole's AllData) are hand-maintained in two different projects with
// nothing connecting them, and they drift in both directions:
//
//   * An entry in the wizard with no registered engine downloads whatever zip was last uploaded,
//     however old, and says nothing. Android and iOS sat that way for the eleven months after the
//     mobile engines stopped building (GitHub issues #1945, #1947).
//   * A registered engine with no wizard entry is built, version-stamped, zipped and FTP'd every
//     release with no way for anyone to select it. The .NET 6 desktop template sat that way.
//
// AllData is read as text rather than referenced: BuildServerUploaderConsole is an exe that project-
// references the DesktopGL engine, which would drag MonoGame into the fast unit run. Same approach
// EnginePackagingTests takes with Engine.yml.
public class NewProjectTemplateListTests
{
    [Fact]
    public void EveryTemplateOfferedByTheWizard_ShouldExistInTheTemplatesFolder()
    {
        foreach (var templateName in WizardTemplateNames())
        {
            var templateDirectory = Path.Combine(RepoPaths.FrbRoot, "Templates", templateName);
            Directory.Exists(templateDirectory).ShouldBeTrue(
                $"The wizard offers '{templateName}' but there is no template at {templateDirectory}, " +
                "so nothing in this repo produces the zip it downloads.");
        }
    }

    [Fact]
    public void EveryTemplateOfferedByTheWizard_ShouldHaveAnEngineTheReleaseUploads()
    {
        var uploaded = TemplatesTheReleaseUploads();

        foreach (var templateName in WizardTemplateNames())
        {
            uploaded.ShouldContain(templateName,
                $"The wizard offers '{templateName}' but AllData does not register its engine, so no " +
                "release uploads that zip. Anyone picking it gets whichever build was uploaded last.");
        }
    }

    [Fact]
    public void EveryTemplateTheReleaseUploads_ShouldBeOfferedByTheWizard()
    {
        var offered = WizardTemplateNames();

        foreach (var templateName in TemplatesTheReleaseUploads())
        {
            offered.ShouldContain(templateName,
                $"AllData registers an engine for '{templateName}', so every release builds, stamps, " +
                "zips and uploads it -- but the wizard has no entry for it, so no one can pick it.");
        }
    }

    // Namespace is the template folder name; it is also the token ProjectCreationHelper replaces with
    // the new project's name. AddNewLocalProjectOption is the "Select Local Project..." row, which
    // points at a folder the user picks rather than at a template in this repo.
    //
    // Both invariants above are about the zip pipeline - a template this repo builds, the release
    // uploads, and the window downloads - so they apply to entries that actually have a zip. A
    // DotnetNewProjectInfo has no Url because the dotnet CLI owns its template, so it has no zip to
    // build and no engine to register. Keyed on the entry having a Url rather than on its type, so a
    // future non-zip template is exempt automatically and every new zip template is still guarded.
    private static HashSet<string> WizardTemplateNames() =>
        EmptyTemplates.Projects
            .Where(project => project is not AddNewLocalProjectOption)
            .Where(project => !string.IsNullOrEmpty(project.Url))
            .Select(project => project.Namespace)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // The template folders belonging to engines AllData actually registers. A block whose
    // Engines.Add is commented out -- how a platform gets disabled without losing the restore
    // instructions -- does not count, which is the whole point of reading it this way.
    private static HashSet<string> TemplatesTheReleaseUploads()
    {
        var allDataPath = Path.Combine(RepoPaths.FrbRoot, "FRBDK", "BuildServerUploader",
            "BuildServerUploaderConsole", "Data", "AllData.cs");
        File.Exists(allDataPath).ShouldBeTrue($"Expected the release engine list at {allDataPath}");

        var templateFolder = new Regex(@"TemplateCsProjFolder\s*=\s*@""([^\\""]+)");
        var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? pendingTemplate = null;

        foreach (var rawLine in File.ReadAllLines(allDataPath))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("//"))
            {
                continue;
            }

            if (line.Contains("new EngineData()"))
            {
                pendingTemplate = null;
            }
            else if (templateFolder.Match(line) is { Success: true } match)
            {
                pendingTemplate = match.Groups[1].Value;
            }
            else if (line.Contains("Engines.Add(engine)") && pendingTemplate != null)
            {
                registered.Add(pendingTemplate);
            }
        }

        registered.ShouldNotBeEmpty("Could not find any registered engines in AllData.cs.");
        return registered;
    }
}
