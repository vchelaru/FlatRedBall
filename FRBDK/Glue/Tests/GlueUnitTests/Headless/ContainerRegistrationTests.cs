using System;
using System.IO;
using System.Linq;
using EditorObjects.IoC;
using FlatRedBall.Glue.Headless;
using FlatRedBall.Glue.SetVariable;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

#pragma warning disable CS0618 // Container is obsolete

namespace GlueUnitTests.Headless;

// Glue.exe and the headless host used to keep separate lists of legacy Container registrations, and
// every type only the editor registered was a headless "container does not contain an entry" crash.
// Both now call GlueHeadlessHost.RegisterContainerServices; these tests keep it that way.
public class ContainerRegistrationTests
{
    public ContainerRegistrationTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public void HeadlessStartup_ResolvesEveryRegisteredType()
    {
        var types = GlueHeadlessHost.RegisteredContainerServiceTypes;

        types.ShouldContain(typeof(SetPropertyManager));
        types.ShouldContain(typeof(StateSaveCategorySetVariableLogic));
        types.ShouldContain(typeof(ScreenSaveSetVariableLogic));

        var get = typeof(Container).GetMethod(nameof(Container.Get))!;
        foreach (var type in types)
        {
            Should.NotThrow(() => get.MakeGenericMethod(type).Invoke(null, null), $"resolving {type}")
                .ShouldNotBeNull();
        }
    }

    // A Container.Set written anywhere else in Glue is a registration the headless host may not make.
    [Fact]
    public void GlueSource_RegistersIntoContainerOnlyThroughSharedMethod()
    {
        var glueDirectory = Path.Combine(GoldProject.FindRepoRoot(), "FRBDK", "Glue", "Glue");
        var sharedFile = Path.Combine(glueDirectory, "Headless", "GlueHeadlessHost.cs");

        var offenders = Directory.EnumerateFiles(glueDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !string.Equals(file, sharedFile, StringComparison.OrdinalIgnoreCase))
            .Where(file => File.ReadAllText(file).Contains("Container.Set"))
            .Select(file => Path.GetRelativePath(glueDirectory, file))
            .ToList();

        offenders.ShouldBeEmpty(
            "Register legacy Container types in GlueHeadlessHost.RegisterContainerServices so the headless host gets them too.");
    }
}
