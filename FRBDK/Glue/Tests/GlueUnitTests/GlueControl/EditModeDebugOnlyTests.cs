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

// #2371: none of the GlueControl code (live edit, the Glue connection, profiling) is compiled into a Release
// build. A Release game never connects to Glue (Game1 creates the connection under #if DEBUG), and the code
// is mostly reflection by name and MakeGenericType that Native AOT cannot compile cleanly. These are the fast
// half: they run the real generators and let Roslyn evaluate the `#if DEBUG` regions, so "what is left in
// Release" is answered without publishing a game. The BuildSmoke EditModeReleaseBuildTests is the half that
// compiles the result.
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

    [Theory]
    [InlineData("Editing.EditingManager.cs", "EditingManager")]
    [InlineData("Editing.VariableAssignmentLogic.cs", "VariableAssignmentLogic")]
    [InlineData("InstanceLogic.cs", "InstanceLogic")]
    [InlineData("Screens.EntityViewingScreen.cs", "EntityViewingScreen")]
    [InlineData("CommandReceiver.cs", "CommandReceiver")]
    [InlineData("GlueControlManager.cs", "GlueControlManager")]
    [InlineData("GameConnectionManager.cs", "GameConnectionManager")]
    [InlineData("Dtos.cs", "ProfilingDataDto")]
    [InlineData("Models.NamedObjectSave.cs", "NamedObjectSave")]
    public void EmbeddedFile_IsCompiledInDebugAndAbsentFromRelease(string file, string expectedType)
    {
        var generated = GlueControlCodeGenerator.GetEmbeddedStringContents(Prefix + file);

        DeclaredTypeNames(generated, "DEBUG").ShouldContain(expectedType);
        DeclaredTypeNames(generated).ShouldBeEmpty();
    }

    [Fact]
    public void EveryEmbeddedFile_IsAbsentFromRelease()
    {
        var resources = typeof(GlueControlCodeGenerator).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix) && name.EndsWith(".cs"))
            .ToList();
        resources.ShouldNotBeEmpty();

        foreach (var resource in resources)
        {
            var generated = GlueControlCodeGenerator.GetEmbeddedStringContents(resource);

            DeclaredTypeNames(generated).ShouldBeEmpty(resource);
            DeclaredTypeNames(generated, "DEBUG").ShouldNotBeEmpty(resource);
        }
    }

    [Fact]
    public void ProfilingHandler_NeedsADebugBuild()
    {
        var generated = GlueControlCodeGenerator.GetEmbeddedStringContents(Prefix + "CommandReceiver.cs");

        Identifiers(generated).ShouldNotContain("GetProfilingDataDto");
        Identifiers(generated, "DEBUG").ShouldContain("GetProfilingDataDto");
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
    public void Game1_GlueControlManagerAndEditorVisuals_OnlyInDebug()
    {
        var generator = new Game1GlueControlGenerator
        {
            IsGlueControlManagerGenerationEnabled = true,
            PortNumber = 8888,
        };
        var block = new CodeDocument(0);

        generator.GenerateInitialize(block);
        generator.GenerateClassScope(block);

        AssertOnlyInDebug(block.ToString(), "GlueControl.Editing.EditorVisuals");
        AssertOnlyInDebug(block.ToString(), "GlueControl.GlueControlManager");
    }

    [Fact]
    public void Game1_ConnectionManager_IsDebugOnly()
    {
        var generator = new Game1GlueCommunicationGenerator(true, 8888);
        var block = new CodeDocument(0);

        generator.GenerateClassScope(block);
        generator.GenerateInitialize(block);

        AssertOnlyInDebug(block.ToString(), "GlueCommunication.GameConnectionManager");
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
