using System;
using System.Collections.Generic;
using System.Linq;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;
using Xunit.Abstractions;

namespace GlueUnitTests.GumPluginTests;

// Issue #2404: lists the Gum schema variables Glue does not emit, so a variable Gum adds is a decision
// (add it to FrbBackedGumVariables, or leave it out) instead of a red CI run. Never fails on what it finds.
// To read it:
//   dotnet test --filter "FullyQualifiedName~GumSchemaVariablesNotGeneratedReport" --logger "console;verbosity=detailed"
[Collection(nameof(TaskManagerSequentialCollection))]
public class GumSchemaVariablesNotGeneratedReport
{
    private readonly ITestOutputHelper _output;

    public GumSchemaVariablesNotGeneratedReport(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ListSchemaVariablesGlueDoesNotGenerate()
    {
        GlueTestBootstrap.EnsureInitialized();
        GlueTestBootstrap.EnsureGumPluginStandardElementsInitialized();

        // The code generators read the loaded project's FileVersion while initializing.
        var originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new FlatRedBall.Glue.SaveClasses.GlueProjectSave
        {
            FileVersion = FlatRedBall.Glue.SaveClasses.GlueProjectSave.LatestVersion,
        };
        try
        {
            GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        }
        finally
        {
            FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = originalGlueProject;
        }

        var manager = Gum.Managers.StandardElementsManager.Self;
        // Surfaces Gum's own shape/Skia schemas (Arc, Svg...), which a current Gum Editor writes.
        manager.RegisterExtendedDefaultStates();

        var elementNames = StandardsCodeGenerator.Self.StandardElementToQualifiedTypes.Keys
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        elementNames.ShouldContain("Text");

        var lines = new List<string>();
        foreach (var elementName in elementNames)
        {
            var schemaVariables = new[]
                {
                    manager.TryGetDefaultStateFor(elementName, throwExceptionOnMissing: false),
                    manager.CustomGetDefaultState?.Invoke(elementName),
                }
                .Where(state => state != null)
                .SelectMany(state => state!.Variables)
                .Select(variable => variable.Name)
                // State references are generated from the element's categories, not from this list.
                .Where(name => !string.IsNullOrEmpty(name) && !name.EndsWith("State", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal);

            var notGenerated = schemaVariables
                .Where(name => !FrbBackedGumVariables.IsAssignableInState(elementName, name))
                .ToList();

            if (notGenerated.Count > 0)
            {
                lines.Add($"{elementName}: {string.Join(", ", notGenerated)}");
            }
        }

        _output.WriteLine("Gum schema variables Glue does not generate (see FrbBackedGumVariables):");
        foreach (var line in lines)
        {
            _output.WriteLine(line);
        }
    }
}
