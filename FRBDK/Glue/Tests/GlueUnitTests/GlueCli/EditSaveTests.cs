using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueCli;
using GlueUnitTests.TestSupport;
using static GlueUnitTests.Cli.GlueCliTestRun;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Cli;

/// <summary>
/// Each edit command, run against a temp copy of Samples/Beefball, then the project reloaded from disk: every
/// element and the .gluj's own data must match the model the edit left in memory, so a change nothing saved fails.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class EditSaveTests
{
    /// <summary>
    /// Commands whose editor code saves what it changes, checked with the runner's own save turned off.
    /// </summary>
    [StaTheory]
    [InlineData("add-object", "--element", "GameScreen", "--type", "Sprite", "--name", "S1")]
    [InlineData("add-object", "--element", "GameScreen", "--type", "Text", "--as-list")]
    [InlineData("add-object", "--element", "GameScreen", "--type", "PlayerBall", "--name", "SecondBall")]
    [InlineData("set-variable", "--element", "GameScreen", "--object", "Wall1", "--variable", "Width", "--value", "512.5")]
    [InlineData("add-entity", "--name", "Coin")]
    [InlineData("add-entity", "--name", "Enemy", "--circle")]
    [InlineData("add-screen", "--name", "TitleScreen")]
    [InlineData("add-variable", "--element", "PlayerBall", "--name", "JumpSpeed", "--type", "float", "--default", "250.5")]
    [InlineData("add-state-category", "--element", "PlayerBall", "--name", "Speed")]
    [InlineData("add-event", "--element", "GameScreen", "--custom", "--name", "MatchEnded")]
    [InlineData("add-file", "--element", "GameScreen", "--file", ScreenPng)]
    [InlineData("add-file", "--global", "--file", GlobalCsv)]
    public void Edit_ShouldSaveWhatItChanged_WithoutTheRunnersSave(string command, params string[] options)
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        WriteContentFiles(project.Root);
        GlueCliRunner.SavesWhatTheEditLeftUnsaved = false;
        try
        {
            RunOk(project.Root, command, options);
        }
        finally
        {
            GlueCliRunner.SavesWhatTheEditLeftUnsaved = true;
        }

        ShouldMatchAfterReload(project.Root);
    }

    /// <summary>Commands the runner saves, or whose editor code saves the whole project.</summary>
    [StaTheory]
    [InlineData("set-property", "--element", "GameScreen", "--object", "Wall1", "--property", "AddToManagers", "--value", "false")]
    [InlineData("set-variable", "--element", "PlayerBall", "--variable", "MovementSpeed", "--value", "350")]
    [InlineData("set-variable-property", "--element", "PlayerBall", "--variable", "MovementSpeed", "--property", "SetByDerived", "--value", "false")]
    [InlineData("remove-variable", "--element", "PlayerBall", "--variable", "CooldownCircleRadius")]
    [InlineData("add-state", "--element", "PlayerBall", "--category", "DashCategory", "--name", "Exhausted")]
    [InlineData("set-state-variable", "--element", "PlayerBall", "--category", "DashCategory", "--state", "Tired", "--variable", "CooldownCircleRadius", "--value", "4.5")]
    [InlineData("rename-state", "--element", "PlayerBall", "--category", "DashCategory", "--state", "Tired", "--to", "Exhausted")]
    [InlineData("delete-state", "--element", "PlayerBall", "--category", "DashCategory", "--state", "Rested")]
    [InlineData("add-collision-relationship", "--element", "GameScreen", "--first", "PlayerBallList", "--second", "GoalList")]
    [InlineData("set-collision-property", "--element", "GameScreen", "--relationship", "PuckVsGoal", "--property", "CollisionType", "--value", "move")]
    [InlineData("rename", "--element", "PlayerBall", "--object", "CooldownCircle", "--to", "DashCircle")]
    [InlineData("rename", "--element", "ScoreHud", "--to", "Scoreboard")]
    [InlineData("delete", "--element", "GameScreen", "--object", "Wall6")]
    [InlineData("delete", "--element", "Puck")]
    public void Edit_ShouldLeaveTheFilesMatchingTheModel(string command, params string[] options)
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");

        RunOk(project.Root, command, options);

        ShouldMatchAfterReload(project.Root);
    }

    [StaFact]
    public void SetFileProperty_ShouldLeaveTheFilesMatchingTheModel()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        WriteContentFiles(project.Root);
        RunOk(project.Root, "add-file", new[] { "--element", "GameScreen", "--file", ScreenPng });

        RunOk(project.Root, "set-file-property", new[] { "--element", "GameScreen", "--file", "Background",
            "--property", "IsSharedStatic", "--value", "false" });

        ShouldMatchAfterReload(project.Root);
    }

    // Under the game's Content folder; written by WriteContentFiles.
    const string ScreenPng = "Beefball/Content/Screens/GameScreen/Background.png";
    const string GlobalCsv = "Beefball/Content/GlobalContent/EnemyInfo.csv";

    static void WriteContentFiles(string root)
    {
        var png = Path.Combine(root, ScreenPng);
        Directory.CreateDirectory(Path.GetDirectoryName(png)!);
        File.WriteAllBytes(png, Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="));
        var csv = Path.Combine(root, GlobalCsv);
        Directory.CreateDirectory(Path.GetDirectoryName(csv)!);
        File.WriteAllText(csv, "\"Name (string, required)\",Speed (float)\r\nSlow,1\r\nFast,5\r\n");
    }

    static void RunOk(string root, string command, string[] options)
    {
        // A relative --file is relative to the project copy, not the test's working directory.
        var resolved = options.Select(option => option.StartsWith("Beefball/") ? Path.Combine(root, option) : option);
        var (exitCode, output) = Run(new[] { command, root }.Concat(resolved).ToArray());
        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
    }

    static void ShouldMatchAfterReload(string root)
    {
        var inMemory = Snapshot();

        var (exitCode, output) = Run("describe", root);
        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var reloaded = Snapshot();

        reloaded.Keys.ShouldBe(inMemory.Keys, ignoreOrder: true);
        foreach (var (name, json) in inMemory)
        {
            reloaded[name].ShouldBe(json, $"{name} on disk does not match the model the edit left in memory.");
        }
    }

    static Dictionary<string, string> Snapshot()
    {
        var project = GlueState.Self.CurrentGlueProject;
        var snapshot = project.Screens.Cast<FlatRedBall.Glue.SaveClasses.GlueElement>().Concat(project.Entities)
            .ToDictionary(element => element.Name, element => Newtonsoft.Json.JsonConvert.SerializeObject(element));
        snapshot.Add("(.gluj)", GlueCliRunner.SerializeProjectLevel());
        return snapshot;
    }
}
