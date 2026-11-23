using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using GlueCli;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;
using static GlueUnitTests.Cli.GlueCliTestRun;

namespace GlueUnitTests.Cli;

/// <summary>
/// <c>GlueCli set-file-property</c> against a temp copy of Samples/Beefball, which references no files, so each test
/// writes its files into the copy and adds them with <c>add-file</c>.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class SetFilePropertyCommandTests
{
    // A valid 1x1 png.
    static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    // A valid 8-sample mono PCM wav, which loads as a SoundEffect and offers SoundEffectInstance too.
    static readonly byte[] Wav = Convert.FromBase64String(
        "UklGRjQAAABXQVZFZm10IBAAAAABAAEAQB8AAIA+AAACABAAZGF0YRAAAAAAAAAAAAAAAAAAAAAAAAAA");

    const string Csv ="Name (string, required),Speed (float)\r\nSlow,1\r\nFast,5\r\n";

    [StaFact]
    public void SetFileProperty_ShouldLoadAnEntityPngOnlyWhenReferenced()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddFile(project, "Entities/PlayerBall/BallTexture.png", Png, "--element", "PlayerBall");

        var (exitCode, output) = Run("set-file-property", project.Root, "--element", "PlayerBall", "--file", "BallTexture",
            "--property", "loadedonlywhenreferenced", "--value", "true", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "file").ShouldBe("Entities/PlayerBall/BallTexture.png");
        EditProperty(result, "property").ShouldBe("LoadedOnlyWhenReferenced");
        EditProperty(result, "oldValue").ShouldBe("False");
        EditProperty(result, "newValue").ShouldBe("True");

        File(ReadJson(project, "Entities", "PlayerBall.glej"), "Entities/PlayerBall/BallTexture.png")
            .GetProperty("LoadedOnlyWhenReferenced").GetBoolean().ShouldBeTrue();
        var generated = System.IO.File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.Generated.cs"));
        generated.ShouldContain("mLastContentManagerForBallTexture", Case.Sensitive);
    }

    [StaFact]
    public void SetFileProperty_ShouldTurnOffIsSharedStaticOnAScreenFile()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddFile(project, "Screens/GameScreen/Background.png", Png, "--element", "GameScreen");

        var (exitCode, output) = Run("set-file-property", project.Root, "--element", "GameScreen", "--file", "Background",
            "--property", "IsSharedStatic", "--value", "false", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        EditProperty(Parse(output), "oldValue").ShouldBe("True");
        File(ReadJson(project, "Screens", "GameScreen.glsj"), "Screens/GameScreen/Background.png")
            .GetProperty("IsSharedStatic").GetBoolean().ShouldBeFalse();
        var generated = System.IO.File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("protected global::Microsoft.Xna.Framework.Graphics.Texture2D Background;", Case.Sensitive);

        // The false survives a reload: the constructor's true used to win because the saved file left the false out.
        var (againExitCode, againOutput) = Run("set-file-property", project.Root, "--element", "GameScreen", "--file", "Background",
            "--property", "IsSharedStatic", "--value", "true", "--json");
        againExitCode.ShouldBe(GlueCliRunner.ExitSuccess, againOutput);
        EditProperty(Parse(againOutput), "oldValue").ShouldBe("False");
    }

    [StaFact]
    public void SetFileProperty_ShouldAnswerTheAddToManagersQuestion_WhenIsSharedStaticTurnsOn()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddFile(project, "Screens/GameScreen/Background.png", Png, "--element", "GameScreen");
        RunOk("set-file-property", project.Root, "--element", "GameScreen", "--file", "Background", "--property", "IsSharedStatic", "--value", "false");
        RunOk("add-object", project.Root, "--element", "GameScreen", "--source-file", "Background.png",
            "--source-name", "Entire File (Texture2D)", "--name", "BackgroundTexture");
        RunOk("set-property", project.Root, "--element", "GameScreen", "--object", "BackgroundTexture",
            "--property", "AddToManagers", "--value", "false");

        var (exitCode, output) = Run("set-file-property", project.Root, "--element", "GameScreen", "--file", "Background",
            "--property", "IsSharedStatic", "--value", "true", "--add-to-managers", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var answer = ((JsonElement)Parse(output).Edit!).GetProperty("answers").EnumerateArray().ToList().ShouldHaveSingleItem();
        answer.GetProperty("question").GetString().ShouldStartWith("The object BackgroundTexture references this file.", Case.Sensitive);
        answer.GetProperty("answered").GetString().ShouldBe("yes (--add-to-managers)");
        var backgroundTexture = ReadJson(project, "Screens", "GameScreen.glsj").GetProperty("NamedObjects").EnumerateArray()
            .Single(nos => nos.GetProperty("InstanceName").GetString() == "BackgroundTexture");
        // AddToManagers is only written when false.
        backgroundTexture.TryGetProperty("AddToManagers", out _).ShouldBeFalse();
    }

    [StaFact]
    public void SetFileProperty_ShouldPickARuntimeTypeFromTheFilesChoices()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddFile(project, "Screens/GameScreen/Hit.wav", Wav, "--element", "GameScreen");

        var (exitCode, output) = Run("set-file-property", project.Root, "--element", "GameScreen", "--file", "Hit",
            "--property", "RuntimeType", "--value", "microsoft.xna.framework.audio.soundeffectinstance", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "oldValue").ShouldBe("Microsoft.Xna.Framework.Audio.SoundEffect");
        EditProperty(result, "newValue").ShouldBe("Microsoft.Xna.Framework.Audio.SoundEffectInstance");
        File(ReadJson(project, "Screens", "GameScreen.glsj"), "Screens/GameScreen/Hit.wav")
            .GetProperty("RuntimeType").GetString().ShouldBe("Microsoft.Xna.Framework.Audio.SoundEffectInstance");
        var generated = System.IO.File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generated.ShouldContain("global::Microsoft.Xna.Framework.Audio.SoundEffectInstance Hit;", Case.Sensitive);
        generated.ShouldContain(".CreateInstance();", Case.Sensitive);
    }

    [StaFact]
    public void SetFileProperty_ShouldRegenerateGlobalContent_ForAGlobalFile()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddFile(project, "GlobalContent/EnemyInfo.csv", Csv, "--global");

        var (exitCode, output) = Run("set-file-property", project.Root, "--global", "--file", "GlobalContent/EnemyInfo.csv",
            "--property", "LoadedOnlyWhenReferenced", "--value", "true", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
        var result = Parse(output);
        EditProperty(result, "element").ShouldBeNull();
        result.FilesWritten.ShouldContain("GlobalContent.Generated.cs");
        File(ReadJson(project, "Beefball.gluj"), "GlobalContent/EnemyInfo.csv", "GlobalFiles")
            .GetProperty("LoadedOnlyWhenReferenced").GetBoolean().ShouldBeTrue();
        var globalContent = System.IO.File.ReadAllText(Path.Combine(project.Root, "Beefball", "GlobalContent.Generated.cs"));
        globalContent.ShouldContain("static global::System.Collections.Generic.List<Beefball.DataTypes.EnemyInfo> mEnemyInfo;", Case.Sensitive);
    }

    [StaFact]
    public void SetFileProperty_ShouldExitOneAndSaveNothing_ForAPropertyTheEditorHidesThere()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddFile(project, "Entities/PlayerBall/BallTexture.png", Png, "--element", "PlayerBall");
        var entityFile = Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.glej");
        var entityBefore = System.IO.File.ReadAllText(entityFile);

        // IsSharedStatic is only shown for a file in a screen.
        var (exitCode, output) = Run("set-file-property", project.Root, "--element", "PlayerBall", "--file", "BallTexture",
            "--property", "IsSharedStatic", "--value", "false", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var failure = Parse(output).EditFailures.ShouldHaveSingleItem();
        failure.ShouldContain("The editor does not show IsSharedStatic", Case.Sensitive);
        failure.ShouldContain("LoadedOnlyWhenReferenced", Case.Sensitive);
        System.IO.File.ReadAllText(entityFile).ShouldBe(entityBefore);
    }

    [StaFact]
    public void SetFileProperty_ShouldExitOneWithCloseMatches_ForAnUnknownProperty()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddFile(project, "Entities/PlayerBall/BallTexture.png", Png, "--element", "PlayerBall");

        var (exitCode, output) = Run("set-file-property", project.Root, "--element", "PlayerBall", "--file", "BallTexture",
            "--property", "LoadedOnlyWhenReferencd", "--value", "true", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var failure = Parse(output).EditFailures.ShouldHaveSingleItem();
        failure.ShouldContain("has no property 'LoadedOnlyWhenReferencd'", Case.Sensitive);
        failure.ShouldContain("Did you mean: LoadedOnlyWhenReferenced?", Case.Sensitive);
    }

    [StaFact]
    public void SetFileProperty_ShouldExitOneAndSaveNothing_ForABadValue()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddFile(project, "Screens/GameScreen/Hit.wav", Wav, "--element", "GameScreen");
        var screenFile = Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj");
        var screenBefore = System.IO.File.ReadAllText(screenFile);

        var (exitCode, output) = Run("set-file-property", project.Root, "--element", "GameScreen", "--file", "Hit",
            "--property", "RuntimeType", "--value", "Microsoft.Xna.Framework.Graphics.Texture2D", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        var failure = Parse(output).EditFailures.ShouldHaveSingleItem();
        failure.ShouldContain("is not a valid value for RuntimeType", Case.Sensitive);
        failure.ShouldContain("Microsoft.Xna.Framework.Audio.SoundEffectInstance", Case.Sensitive);
        System.IO.File.ReadAllText(screenFile).ShouldBe(screenBefore);
    }

    [StaFact]
    public void SetFileProperty_ShouldExitOne_ForAnUnknownFile()
    {
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        AddFile(project, "Entities/PlayerBall/BallTexture.png", Png, "--element", "PlayerBall");

        var (exitCode, output) = Run("set-file-property", project.Root, "--element", "PlayerBall", "--file", "BallTextur",
            "--property", "LoadedAtRuntime", "--value", "false", "--json");

        exitCode.ShouldBe(GlueCliRunner.ExitErrors, output);
        Parse(output).EditFailures.ShouldHaveSingleItem().ShouldContain("Entities/PlayerBall/BallTexture.png", Case.Sensitive);
    }

    static void AddFile(TempDir project, string relativePath, object contents, params string[] container)
    {
        var path = Path.Combine(project.Root, "Beefball", "Content", relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (contents is byte[] bytes)
        {
            System.IO.File.WriteAllBytes(path, bytes);
        }
        else
        {
            System.IO.File.WriteAllText(path, (string)contents);
        }
        var (exitCode, output) = Run(new[] { "add-file", project.Root }.Concat(container).Concat(new[] { "--file", path }).ToArray());
        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
    }

    static void RunOk(params string[] args)
    {
        var (exitCode, output) = Run(args);
        exitCode.ShouldBe(GlueCliRunner.ExitSuccess, output);
    }

    static JsonElement ReadJson(TempDir project, params string[] pathInProject) =>
        JsonDocument.Parse(System.IO.File.ReadAllText(Path.Combine(new[] { project.Root, "Beefball" }.Concat(pathInProject).ToArray()))).RootElement;

    static JsonElement File(JsonElement owner, string name, string property = "ReferencedFiles") =>
        owner.GetProperty(property).EnumerateArray().Single(file => file.GetProperty("Name").GetString() == name);
}
