using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.CodeGeneration;
using GameCommunicationPlugin.GlueControl.CodeGeneration;
using GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls;
using GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls.GenerationConfiguration;
using GlueUnitTests.TestSupport;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GlueControl;

// #2371: live edit (the edit mode code embedded into a game) is compiled into Debug builds only, because
// it is mostly reflection by name and MakeGenericType that Native AOT cannot compile cleanly and a shipped
// game has no use for. The Glue connection and the profiling data it answers with stay in every
// configuration. These are the fast half: they run the real generators and let Roslyn evaluate the
// `#if DEBUG` regions, so "what is left in Release" is answered without publishing a game. The BuildSmoke
// EditModeReleaseBuildTests is the half that compiles the result.
public class EditModeDebugOnlyTests : IDisposable
{
    const string Prefix = "GameCommunicationPlugin.GlueControl.Embedded.";

    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _tempProjectDirectory;

    public EditModeDebugOnlyTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = (int)GlueProjectSave.LatestVersion };
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        try { Directory.Delete(_tempProjectDirectory, recursive: true); } catch { }
    }

    #region Embedded files

    [Fact]
    public void SupportsEditModeDefine_IsDebugOnly()
    {
        var generated = GlueControlCodeGenerator.GetEmbeddedStringContents(Prefix + "Editing.CameraLogic.cs");

        // The define is the whole point of the `#if SupportsEditMode` regions inside the Release files, so
        // check what it evaluates to rather than where it sits in the text.
        var probe = generated + "\r\n#if SupportsEditMode\r\nclass SupportsEditModeProbe { }\r\n#endif\r\n";

        DeclaredTypeNames(probe, "DEBUG").ShouldContain("SupportsEditModeProbe");
        DeclaredTypeNames(probe).ShouldNotContain("SupportsEditModeProbe");
    }

    [Theory]
    [InlineData("Editing.EditingManager.cs", "EditingManager")]
    [InlineData("Editing.VariableAssignmentLogic.cs", "VariableAssignmentLogic")]
    [InlineData("Editing.SelectionLogic.cs", "SelectionLogic")]
    [InlineData("Editing.CameraLogic.cs", "CameraLogic")]
    [InlineData("Editing.EditorVisuals.cs", "EditorVisuals")]
    [InlineData("InstanceLogic.cs", "InstanceLogic")]
    [InlineData("CommandReplayLogic.cs", "CommandReplayLogic")]
    [InlineData("Screens.EntityViewingScreen.cs", "EntityViewingScreen")]
    [InlineData("Runtime.DynamicEntity.cs", "DynamicEntity")]
    [InlineData("Models.NamedObjectSaveExtensionMethods.cs", "NamedObjectSaveExtensionMethods")]
    public void EditModeFile_IsCompiledInDebugAndAbsentFromRelease(string file, string expectedType)
    {
        var generated = GlueControlCodeGenerator.GetEmbeddedStringContents(Prefix + file);

        DeclaredTypeNames(generated, "DEBUG").ShouldContain(expectedType);
        DeclaredTypeNames(generated).ShouldBeEmpty();
    }

    [Fact]
    public void EveryEmbeddedFile_IsEitherAbsentFromRelease_OrOnTheReleaseList()
    {
        var resources = typeof(GlueControlCodeGenerator).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix) && name.EndsWith(".cs"))
            .ToList();
        resources.ShouldNotBeEmpty();

        var releaseFiles = new List<string>();
        foreach (var resource in resources)
        {
            var generated = GlueControlCodeGenerator.GetEmbeddedStringContents(resource);
            var inRelease = DeclaredTypeNames(generated).Any();
            inRelease.ShouldBe(!GlueControlCodeGenerator.IsEditModeOnly(resource), resource);
            if (inRelease)
            {
                releaseFiles.Add(resource.Substring(Prefix.Length));
            }
        }

        // The connection to Glue and the data profiling answers with. If this list grows, the new file has
        // to be as safe to publish with Native AOT as these are, which is not a call to make by accident.
        releaseFiles.ShouldBe(new[]
        {
            "CommandReceiver.cs",
            "Dtos.cs",
            "GameConnectionManager.cs",
            "GlueControlManager.cs",
            "Models.CustomVariable.cs",
            "Models.GlueElement.cs",
            "Models.GlueElementFileReference.cs",
            "Models.GlueProjectSave.cs",
            "Models.IElementExtensionMethods.cs",
            "Models.INamedObjectContainer.cs",
            "Models.NamedObjectSave.cs",
            "Models.ReferencedFileSave.cs",
            "Models.StateSave.cs",
            "Models.StateSaveCategory.cs",
        }, ignoreOrder: true);
    }

    [Fact]
    public void CommandReceiver_ProfilingHandler_IsOutsideTheEditModeRegion()
    {
        var generated = GlueControlCodeGenerator.GetEmbeddedStringContents(Prefix + "CommandReceiver.cs");

        var release = Identifiers(generated);
        release.ShouldContain("GetProfilingDataDto");
        release.ShouldContain("GetCollisionDataDto");
        release.ShouldContain("Receive");

        // And the live edit handlers are what went.
        release.ShouldNotContain("VariableAssignmentLogic");
        release.ShouldNotContain("EditingManager");
        release.ShouldNotContain("SetEditMode");
        release.ShouldNotContain("SelectObjectDto");

        var debug = Identifiers(generated, "DEBUG");
        debug.ShouldContain("GetProfilingDataDto");
        debug.ShouldContain("VariableAssignmentLogic");
        debug.ShouldContain("SetEditMode");
    }

    [Fact]
    public void GlueControlManager_InReleaseDoesNotCreateTheEditingManager()
    {
        var generated = GlueControlCodeGenerator.GetEmbeddedStringContents(Prefix + "GlueControlManager.cs");

        Identifiers(generated).ShouldContain("SendToGlue");
        Identifiers(generated).ShouldNotContain("EditingManager");
        Identifiers(generated, "DEBUG").ShouldContain("EditingManager");
    }

    #endregion

    #region Generated game code

    [Fact]
    public void ApplyEditorCommands_CallIntoInstanceLogic_OnlyInDebug()
    {
        var generator = new ApplyEditorCommandsCodeGenerator { IsGameCommunicationEnabled = true };

        var entityBlock = new CodeDocument(0);
        generator.GenerateAddToManagers(entityBlock, new EntitySave { Name = "Player" });
        AssertOnlyInDebug(entityBlock.ToString(), "GlueControl.InstanceLogic");

        var screenBlock = new CodeDocument(0);
        generator.GenerateAddToManagers(screenBlock, new ScreenSave { Name = "MenuScreen" });
        AssertOnlyInDebug(screenBlock.ToString(), "GlueControl.InstanceLogic");
    }

    [Fact]
    public void ReflectionStateAssignment_CallsIntoVariableAssignmentLogic_OnlyInDebug()
    {
        var original = GameJsonCommunicationPlugin.Common.GameConnectionManager.Self;
        var connection = new GameJsonCommunicationPlugin.Common.GameConnectionManager((_, __) => { }, 0);
        try
        {
            GameJsonCommunicationPlugin.Common.GameConnectionManager.Self = connection;
            var block = new CodeDocument(0);

            ReflectionBasedStateCodeGenerator.AddReflectionStateAssignmentCode(block, new StateSaveCategory { Name = "VariableState" });

            var code = block.ToString();
            AssertOnlyInDebug(code, "GlueControl.InstanceLogic");
            AssertOnlyInDebug(code, "GlueControl.Editing.VariableAssignmentLogic");
        }
        finally
        {
            GameJsonCommunicationPlugin.Common.GameConnectionManager.Self = original;
            connection.Dispose();
        }
    }

    [Fact]
    public void Game1_AfterScreenDestroyedHook_CallsEditorVisuals_OnlyInDebug()
    {
        var generator = new Game1GlueControlGenerator
        {
            IsGlueControlManagerGenerationEnabled = true,
            PortNumber = 8888,
            // The setting that drops the connection's own #if DEBUG; the editor visuals must not depend on it.
            GenerateConnectionOnlyInDebug = false,
        };
        var block = new CodeDocument(0);

        generator.GenerateInitialize(block);

        AssertOnlyInDebug(block.ToString(), "GlueControl.Editing.EditorVisuals");
    }

    [Fact]
    public void GlueCallsClasses_AreDebugOnly()
    {
        var generated = GlueCallsCodeGenerator.GenerateGlueCommunicationClass(
            Editing_Managers_GlueState.GetGenerationOptions());

        // Defines have to precede the first token, so they stay outside the region.
        DeclaredTypeNames(generated, "DEBUG").ShouldContain("GlueState");
        DeclaredTypeNames(generated).ShouldBeEmpty();
    }

    // Asserts every line containing `reference` sits inside an `#if DEBUG` region.
    static void AssertOnlyInDebug(string code, string reference)
    {
        var lines = code.Replace("\r", "").Split('\n');
        var depthOfDebug = new Stack<bool>();
        var found = false;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("#if "))
            {
                depthOfDebug.Push(line == "#if DEBUG");
            }
            else if (line.StartsWith("#endif"))
            {
                depthOfDebug.Pop();
            }
            else if (line.Contains(reference))
            {
                found = true;
                depthOfDebug.Contains(true).ShouldBeTrue($"'{line}' is not inside #if DEBUG:\n{code}");
            }
        }
        found.ShouldBeTrue($"Expected the generated code to reference {reference}:\n{code}");
        depthOfDebug.ShouldBeEmpty("Unbalanced #if/#endif in:\n" + code);
    }

    #endregion

    static SyntaxNode Parse(string source, params string[] symbols) =>
        CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(preprocessorSymbols: symbols)).GetRoot();

    static List<string> DeclaredTypeNames(string source, params string[] symbols) =>
        Parse(source, symbols).DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Select(item => item.Identifier.Text)
            .Distinct()
            .ToList();

    static HashSet<string> Identifiers(string source, params string[] symbols) =>
        Parse(source, symbols).DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken))
            .Select(token => token.Text).ToHashSet();
}
