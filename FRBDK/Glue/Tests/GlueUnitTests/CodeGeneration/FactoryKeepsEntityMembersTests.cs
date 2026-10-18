using System;
using System.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.FactoryPlugin;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CodeGeneration;

// Pins #2368: at project version FactoriesKeepEntityMembers an entity's factory names the entity's public fields and
// properties with [DynamicDependency], so the Tiled entity instantiator can set them by name under Native AOT.
// Older versions keep the factory as it was, because the attribute needs .NET 5 or newer.
public class FactoryKeepsEntityMembersTests : IDisposable
{
    const int KeepsMembersVersion = (int)GlueProjectSave.GluxVersions.FactoriesKeepEntityMembers;

    readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    readonly GlueProjectSave _originalGlueProject;
    readonly string _tempProjectDirectory;

    public FactoryKeepsEntityMembersTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;

        try { Directory.Delete(_tempProjectDirectory, recursive: true); }
        catch { /* best-effort cleanup */ }
    }

    static string GetFactoryCode(int fileVersion, bool pooled = false)
    {
        var entity = new EntitySave { Name = "Entities\\Enemy", PooledByFactory = pooled };
        var project = new GlueProjectSave { FileVersion = fileVersion };
        project.Entities.Add(entity);
        ObjectFinder.Self.GlueProject = project;

        return new FactoryEntireClassGenerator { EntitySave = entity }.GetCode();
    }

    const string ExpectedAttribute =
        "[System.Diagnostics.CodeAnalysis.DynamicDependency(" +
        "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | " +
        "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties, typeof(Enemy))]";

    [Fact]
    public void Factory_ShouldKeepEntityMembers_AtTheKeepVersion()
    {
        GetFactoryCode(KeepsMembersVersion).ShouldContain(ExpectedAttribute, Case.Sensitive);
    }

    [Fact]
    public void Factory_ShouldKeepEntityMembers_WhenPooled()
    {
        GetFactoryCode(KeepsMembersVersion, pooled: true).ShouldContain(ExpectedAttribute, Case.Sensitive);
    }

    [Fact]
    public void Factory_ShouldPutTheAttributeOnTheMethodThatCreatesTheEntity()
    {
        var code = GetFactoryCode(KeepsMembersVersion);

        var attributeIndex = code.IndexOf(ExpectedAttribute, StringComparison.Ordinal);
        var createNewIndex = code.IndexOf("CreateNew (Layer layer, float? x = null", attributeIndex, StringComparison.Ordinal);
        var newInstanceIndex = code.IndexOf("instance = new Enemy(", attributeIndex, StringComparison.Ordinal);

        attributeIndex.ShouldBeGreaterThan(-1);
        createNewIndex.ShouldBeGreaterThan(attributeIndex);
        // Only whitespace and the method signature sit between the attribute and the method that news up the entity.
        code.Substring(attributeIndex + ExpectedAttribute.Length, createNewIndex - attributeIndex - ExpectedAttribute.Length)
            .ShouldNotContain("}");
        newInstanceIndex.ShouldBeGreaterThan(createNewIndex);
    }

    [Fact]
    public void Factory_ShouldNotUseTheAttribute_BeforeTheKeepVersion()
    {
        GetFactoryCode(KeepsMembersVersion - 1).ShouldNotContain("DynamicDependency");
    }
}
