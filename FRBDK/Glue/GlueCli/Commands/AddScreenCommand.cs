using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using GlueFormsCore.ViewModels;

namespace GlueCli.Commands;

/// <summary>
/// <c>add-screen</c>: adds a screen through
/// <see cref="FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.IScreenCommands.AddScreenAsync"/>, the
/// call the editor's Add Screen dialog ends in, so plugins react (the Gum plugin's paired Gum screen) and code
/// generates as in the editor. --base sets BaseScreen afterwards and runs the editor's property-change reaction,
/// as the dialog's "inherit from GameScreen" level option does.
/// </summary>
public static class AddScreenCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "add-screen",
        Summary = "Add a screen, optionally inheriting from another screen.",
        Usage = "add-screen <project> --name <name> [--folder <folder>] [--base <screen>]",
        Description =
@"Adds the screen the way the editor's Add Screen dialog does: same files, same code generation, same plugin
reactions (for a project with Gum, a paired <Name>Gum screen). Then saves the .gluj, the new .glsj and the
.csproj, and regenerates code. The first screen in a project becomes the startup screen.

The dialog's level options (tile map, collision, copying another level's entities, startup screen) are not
applied; add those objects with add-object. With --base the screen inherits that screen's objects and files.

The run fails (exit 1, nothing saved) when the name is invalid or taken by a screen, entity or file, the folder
name is invalid, or the base screen is unknown. The message lists close matches.

With --json, ""edit"" holds the new screen: {name, baseScreen, isStartupScreen, objects}, where objects are the
screen's own object names (the Gum screen object, for example).",
        Options = new CliOption[]
        {
            new("name", "name", "Screen name without folders (Level2). Required."),
            new("folder", "folder", "Folder under Screens (Levels). Default: Screens itself."),
            new("base", "screen", "Screen to inherit from (GameScreen, Screens\\GameScreen)."),
        },
        Examples = new[]
        {
            ("add-screen MyGame --name TitleScreen", "Add Screens\\TitleScreen."),
            ("add-screen MyGame --name Level1 --base GameScreen --json", "Add Level1 inheriting from GameScreen, JSON output."),
        },
        IsEdit = true,
        ValidateArguments = context => ElementNaming.ValidateNameArgument(context, "add-screen"),
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record AddedScreen(string Name, string? BaseScreen, bool IsStartupScreen, List<string> Objects);

    static async Task RunAsync(CliContext context)
    {
        var name = context.Get("name")!;
        if (!Builder.Get<NameVerifier>().IsScreenNameValid(name, null, out var whyNameIsInvalid))
        {
            context.Fail($"'{name}' is not a valid screen name: {whyNameIsInvalid}");
            return;
        }

        var directory = ElementNaming.ResolveDirectory(context, "Screens");
        if (directory == null)
        {
            return;
        }

        var project = GlueState.Self.CurrentGlueProject;
        ScreenSave? baseScreen = null;
        if (context.Has("base"))
        {
            baseScreen = ElementNaming.FindElement(context.Get("base")!, project.Screens, "screen", out var baseError);
            if (baseScreen == null)
            {
                context.Fail(baseError!);
                return;
            }
        }

        var viewModel = new AddScreenViewModel
        {
            ScreenName = name,
            Directory = directory,
        };
        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreenAsync(viewModel);

        if (baseScreen != null)
        {
            await TaskManager.Self.AddAsync(async () =>
            {
                screen.BaseScreen = baseScreen.Name;
                await GlueCommands.Self.GluxCommands.ElementCommands.ReactToPropertyChanged(screen, nameof(screen.BaseScreen), null);
            }, $"Setting the base screen of {screen} to {baseScreen}");
        }

        GlueState.Self.CurrentElement = screen;
        // Plugin reactions (the Gum screen object) queue their own tasks; report what they added too.
        await TaskManager.Self.WaitForAllTasksFinished();

        var added = new AddedScreen(
            Name: screen.Name,
            BaseScreen: string.IsNullOrEmpty(screen.BaseScreen) ? null : screen.BaseScreen,
            IsStartupScreen: project.StartUpScreen == screen.Name,
            Objects: screen.NamedObjects.Select(nos => nos.InstanceName).ToList());

        context.Report(added, $"Added screen {added.Name}" +
            (added.BaseScreen != null ? $", inheriting {added.BaseScreen}" : "") +
            (added.IsStartupScreen ? ", the startup screen" : "") + "." +
            (added.Objects.Count > 0 ? " Objects: " + string.Join(", ", added.Objects) + "." : ""));
    }
}
