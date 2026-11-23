using System.IO;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueFormsCore.ViewModels;

namespace GlueCli.Commands;

/// <summary>
/// <c>delete</c>: removes a screen, an entity, an object or a file through the editor's own delete flow,
/// <c>DialogCommands.AskToRemove*Async</c>: plan the delete (<see cref="DeletionPlanner"/>), show the one
/// delete dialog, run the <c>GluxCommands</c> removal, then apply the dialog's file action. Instead of a
/// window, <see cref="DialogService.ShowDeleteImpl"/> answers the dialog from this command's flags, so
/// references, derived elements, code files and plugin reactions change exactly as in the editor.
/// </summary>
public static class DeleteCommand
{
    // MainGumPlugin's tag for its "also delete the Gum screen" option. GlueCli does not reference the Gum plugin.
    const string GumScreenOptionTag = "GumPlugin.DeleteGumScreen";

    static readonly Dictionary<string, FileDeleteAction> FileActions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["leave"] = FileDeleteAction.Nothing,
        ["remove-from-project"] = FileDeleteAction.RemoveFromProject,
        ["delete"] = FileDeleteAction.RemoveAndDelete,
    };

    public static readonly CliCommand Definition = new()
    {
        Name = "delete",
        Summary = "Delete a screen, an entity, an object in one, or a file (from an element or Global Content).",
        Usage = "delete <project> (--element <element> [--object <object> | --file <file>] | --global --file <file>) [--files <action>] [--keep-inheritance] [--keep-objects] [--keep-gum-screen]",
        Description =
@"Deletes the way the editor's Delete does, answering its delete dialog from the flags below. What goes with
the target goes as in the editor: deleting an entity removes every object of that entity in other screens
and entities, deleting an object removes its collision relationships, tunneled variables and event
responses, and deleting an element resets the base of the elements that inherit from it. Then saves the
.gluj and element files and regenerates code.

--element alone deletes that screen or entity. With --object it deletes that object in the element (lists
and objects inside lists too); with --file, that file of the element. --global --file deletes a Global
Content file. <file> is the file's name in Glue (Screens/GameScreen/Background.png), its name in code
(Background), or its path on disk.

The delete dialog's choices, as flags:
  --files <action>    What happens to the files the delete leaves unused (the element's .cs, .Generated.cs,
                      factory and .glej/.glsj, a removed file itself, a CSV's data class):
                      remove-from-project (default) takes them out of the .csproj and keeps them on disk,
                      delete also moves them to the recycle bin, leave keeps them in the .csproj too.
                      The editor's dialog defaults to delete; this defaults to keeping the files on disk.
                      Either way an entity's factory (Factories/<Name>Factory.Generated.cs) is deleted,
                      as in the editor: removing the entity removes its factory whatever the dialog says.
  --keep-inheritance  Do not reset the base of elements that inherit from a deleted element (the dialog's
                      ""Reset the inheritance for X"", checked by default). They are left with a missing base.
  --keep-objects      When deleting a file, keep the objects that use it (the dialog's ""Remove the object X,
                      which references this file"", checked by default). Their file reference is cleared.
  --keep-gum-screen   When deleting a screen, keep its Gum screen (the Gum plugin's option, checked by default).
Any other option a plugin adds keeps the dialog's default and is listed in the output.

The run fails (exit 1, changes not saved) when the element, object or file does not exist (the message lists
close matches) or when the object is defined by a base element that exposes it to derived elements.

With --json, ""edit"" holds {kind, name, element, alsoRemoved, options, warnings, fileAction, files}: kind is
screen, entity, object or file, element is the containing element (null for a deleted element or a Global
Content file), alsoRemoved lists what the delete removed with it, options are the dialog's options with
{text, checked}, warnings are what the dialog would have warned about, fileAction is the --files value, and
files lists each left-over file as {path, onDisk, inProject} after the delete, path relative to the .csproj
folder.",
        Options = new CliOption[]
        {
            new("element", "element", "The screen or entity to delete, or that holds the object or file."),
            new("global", null, "The file is in Global Content (with --file)."),
            new("object", "object", "Delete this object of the element instead of the element."),
            new("file", "file", "Delete this file of the element (or of Global Content with --global)."),
            new("files", "action", "remove-from-project (default), delete, or leave: what happens to left-over files."),
            new("keep-inheritance", null, "Leave elements that inherit from a deleted element with their base."),
            new("keep-objects", null, "When deleting a file, keep the objects that use it."),
            new("keep-gum-screen", null, "When deleting a screen, keep its Gum screen."),
        },
        Examples = new[]
        {
            ("delete MyGame --element GameScreen --object Enemy1", "Delete the object Enemy1 from GameScreen."),
            ("delete MyGame --element Entities\\Bullet --json", "Delete the Bullet entity and its instances, keep its files on disk, JSON output."),
            ("delete MyGame --element Level1 --files delete", "Delete the Level1 screen and move its code and JSON files to the recycle bin."),
            ("delete MyGame --global --file GlobalContent/EnemyInfo.csv", "Delete a Global Content CSV and the objects using it."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record Deleted(string Kind, string Name, string? Element, List<string> AlsoRemoved, List<DeleteOption> Options,
        List<string> Warnings, string FileAction, List<LeftOverFile> Files);

    public sealed record DeleteOption(string Text, bool Checked);

    public sealed record LeftOverFile(string Path, bool OnDisk, bool InProject);

    static string? ValidateArguments(CliContext context)
    {
        if (context.Has("global"))
        {
            if (context.Has("element") || context.Has("object") || !context.Has("file"))
            {
                return "--global needs --file and no --element or --object.";
            }
        }
        else if (!context.Has("element"))
        {
            return "delete needs --element, or --global with --file.";
        }
        if (context.Has("object") && context.Has("file"))
        {
            return "Pass --object or --file, not both.";
        }
        if (context.Has("files") && !FileActions.ContainsKey(context.Get("files")!))
        {
            return $"--files must be one of {string.Join(", ", FileActions.Keys)}.";
        }
        return null;
    }

    static async Task RunAsync(CliContext context)
    {
        GlueElement? element = null;
        if (context.Has("element"))
        {
            element = CliLookup.FindElement(context.Get("element")!, out var elementError);
            if (element == null)
            {
                context.Fail(elementError!);
                return;
            }
        }
        GlueState.Self.CurrentElement = element;

        string kind;
        string name;
        Func<Task<bool>> delete;
        var dialogs = GlueCommands.Self.DialogCommands;

        if (context.Has("object"))
        {
            var nos = FindObject(context, element!);
            if (nos == null)
            {
                return;
            }
            var definingObject = nos.DefinedByBase ? ObjectFinder.Self.GetRootDefiningObject(nos) : null;
            if (definingObject?.ExposedInDerived == true || definingObject?.SetByDerived == true)
            {
                // The editor shows this as a message and deletes nothing (DialogCommands.CanRemoveObjects).
                context.Fail($"{nos.InstanceName} is defined by a base element that exposes it to derived elements " +
                    $"({definingObject}), so it can only be deleted there.");
                return;
            }
            (kind, name) = ("object", nos.InstanceName);
            delete = () => dialogs.AskToRemoveObjectAsync(nos);
        }
        else if (context.Has("file"))
        {
            var rfs = FindFile(context, element);
            if (rfs == null)
            {
                return;
            }
            (kind, name) = ("file", rfs.Name);
            delete = () => dialogs.AskToRemoveReferencedFileAsync(rfs);
        }
        else if (element is ScreenSave screen)
        {
            (kind, name) = ("screen", screen.Name);
            delete = () => dialogs.AskToRemoveScreenAsync(screen);
        }
        else
        {
            var entity = (EntitySave)element!;
            (kind, name) = ("entity", entity.Name);
            delete = () => dialogs.AskToRemoveEntityAsync(entity);
        }

        // Removing a file takes the content files it leaves unreferenced out of the .csproj, found by comparing
        // against this list. The editor records it when it opens the project; the CLI's model-only load does not.
        ProjectLoader.Self.Operations.RefreshUnreferencedFiles();
        await TaskManager.Self.WaitForAllTasksFinished();

        var fileAction = FileActions[context.Get("files") ?? "remove-from-project"];
        DeleteOptionsViewModel? answered = null;
        var previousShowDelete = DialogService.ShowDeleteImpl;
        DialogService.ShowDeleteImpl = viewModel =>
        {
            Answer(context, viewModel, fileAction);
            answered = viewModel;
            return true;
        };
        bool wasDeleted;
        try
        {
            wasDeleted = await delete();
        }
        finally
        {
            DialogService.ShowDeleteImpl = previousShowDelete;
        }

        if (!wasDeleted || answered == null)
        {
            context.Fail($"Glue did not delete {name}.");
            return;
        }

        var deleted = new Deleted(
            Kind: kind,
            Name: name,
            Element: kind is "object" or "file" ? element?.Name : null,
            AlsoRemoved: answered.ObjectsToRemove.ToList(),
            Options: answered.Options.Select(option => new DeleteOption(option.Display, option.IsChecked)).ToList(),
            Warnings: answered.Warnings.ToList(),
            FileAction: FileActions.First(pair => pair.Value == fileAction).Key,
            Files: DescribeLeftOverFiles(answered.FilesToRemove));

        context.Report(deleted, DescribeText(deleted));
    }

    /// <summary>What a user clicking through the delete dialog with these flags would leave checked.</summary>
    static void Answer(CliContext context, DeleteOptionsViewModel viewModel, FileDeleteAction fileAction)
    {
        foreach (var option in viewModel.Options)
        {
            var keep = option.Tag switch
            {
                DeletionPlanner.ResetInheritanceTag => context.Has("keep-inheritance"),
                DeletionPlanner.RemoveNamedObjectTag => context.Has("keep-objects"),
                GumScreenOptionTag => context.Has("keep-gum-screen"),
                _ => false,
            };
            if (keep)
            {
                option.IsChecked = false;
            }
        }
        viewModel.FileAction = fileAction;
    }

    static NamedObjectSave? FindObject(CliContext context, GlueElement element)
    {
        var objectName = context.Get("object")!;
        var allObjects = element.AllNamedObjects.ToList();
        var nos = allObjects.FirstOrDefault(item => item.InstanceName == objectName) ??
            allObjects.FirstOrDefault(item => string.Equals(item.InstanceName, objectName, StringComparison.OrdinalIgnoreCase));
        if (nos == null)
        {
            context.Fail($"{element.Name} has no object '{objectName}'." +
                (allObjects.Count == 0 ? " It has no objects." : CliLookup.DescribeCloseMatches(objectName, allObjects.Select(item => item.InstanceName))));
        }
        return nos;
    }

    /// <summary>
    /// The file <c>--file</c> names in <paramref name="element"/> (Global Content when null): its Glue name
    /// (either slash), its name in code when only one file has it, or its path on disk.
    /// </summary>
    internal static ReferencedFileSave? FindFile(CliContext context, GlueElement? element)
    {
        var argument = context.Get("file")!;
        var files = element != null ? element.ReferencedFiles : GlueState.Self.CurrentGlueProject.GlobalFiles;
        var normalized = argument.Replace('\\', '/');

        var match = files.FirstOrDefault(rfs => string.Equals(rfs.Name, normalized, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            var byCodeName = files.Where(rfs => string.Equals(rfs.GetInstanceName(), argument, StringComparison.OrdinalIgnoreCase)).ToList();
            match = byCodeName.Count == 1 ? byCodeName[0] : null;
        }
        if (match == null && TryGetPath(argument) is { } path)
        {
            match = files.FirstOrDefault(rfs => GlueCommands.Self.FileCommands.GetFilePath(rfs) == path);
        }
        if (match == null)
        {
            var containerName = element?.Name ?? "Global Content";
            context.Fail($"{containerName} has no file '{argument}'." +
                (files.Count == 0 ? " It has no files." : CliLookup.DescribeCloseMatches(argument, files.Select(rfs => rfs.Name))));
        }
        return match;
    }

    static FilePath? TryGetPath(string argument)
    {
        try
        {
            var roots = Path.IsPathRooted(argument) ? new[] { "" } : new[] { Directory.GetCurrentDirectory(), GlueState.Self.ContentDirectory };
            foreach (var root in roots)
            {
                var candidate = Path.GetFullPath(Path.Combine(root, argument));
                if (File.Exists(candidate))
                {
                    return new FilePath(CliLookup.ToLongPath(candidate));
                }
            }
        }
        catch (ArgumentException)
        {
            // Not a path at all.
        }
        return null;
    }

    static List<LeftOverFile> DescribeLeftOverFiles(IEnumerable<string> files)
    {
        var projectRoot = GlueState.Self.CurrentGlueProjectDirectory;
        var projects = new[] { GlueState.Self.CurrentMainProject, GlueState.Self.CurrentMainContentProject }
            .Where(project => project != null)
            .Distinct()
            .ToList();
        return files
            .Select(file => new FilePath(file))
            .Distinct()
            .Select(file => new LeftOverFile(
                Path: FileManager.MakeRelative(file.FullPath, projectRoot).Replace('\\', '/'),
                OnDisk: file.Exists(),
                InProject: projects.Any(project => project.IsFilePartOfProject(file.FullPath))))
            .ToList();
    }

    static string DescribeText(Deleted deleted)
    {
        var lines = new List<string>
        {
            $"Deleted {deleted.Kind} {deleted.Name}" + (deleted.Element != null ? $" from {deleted.Element}." : "."),
        };
        if (deleted.AlsoRemoved.Count > 0)
        {
            lines.Add("Also removed: " + string.Join(", ", deleted.AlsoRemoved));
        }
        foreach (var option in deleted.Options)
        {
            lines.Add($"[{(option.Checked ? "x" : " ")}] {option.Text}");
        }
        lines.AddRange(deleted.Warnings.Select(warning => "Warning: " + warning));
        if (deleted.Files.Count > 0)
        {
            lines.Add($"Left-over files (--files {deleted.FileAction}):");
            lines.AddRange(deleted.Files.Select(file =>
                $"  {file.Path}: {(file.OnDisk ? "kept on disk" : "deleted")}, {(file.InProject ? "still in the .csproj" : "not in the .csproj")}"));
        }
        return string.Join(Environment.NewLine, lines);
    }
}
