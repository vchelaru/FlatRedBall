using System.IO;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using FlatRedBall.IO;

namespace GlueCli.Commands;

/// <summary>
/// <c>rename</c>: renames a screen or entity, an object, or a file through the calls the tree view's in-place
/// rename ends in (<c>NodeViewModel.HandleRenameThroughEdit</c>): ElementCommands.RenameElement,
/// GluxCommands.RenameNamedObjectSave and FileCommands.RenameReferencedFileSave. Those validate by showing a
/// message and reverting, so the name checks run here first. Their two questions ("overwrite existing code
/// files?", "use the existing file?") are answered from --overwrite and --use-existing-file.
/// </summary>
public static class RenameCommand
{
    const string OverwriteQuestion = "This rename would result in existing files being overwritten.";
    const string ExistingFileQuestion = "The new file name already exists.";
    const string UseExistingFileLabel = "Use existing file";
    const string CancelRenameLabel = "Cancel the rename";

    public static readonly CliCommand Definition = new()
    {
        Name = "rename",
        Summary = "Rename a screen, an entity, an object in one, or a file, updating everything that refers to it.",
        Usage = "rename <project> (--element <element> [--object <name>] | --file <file> (--element <element> | --global)) --to <new name> [options]",
        Description =
@"Renames the way the editor's tree view does (click a selected item, type the new name), with the same
updates and plugin reactions. Then saves the .gluj and element files and regenerates the changed code.

--element alone renames that screen or entity. It stays in its folder: Entities\Enemies\Slime --to Blob
becomes Entities\Enemies\Blob. Its code files move and their class names change; derived elements, objects
of its type, lists of it, collision relationships, variables of its type and the startup screen follow.
Hand-written code elsewhere that names the old class is not changed.

--element with --object renames an object in that element. Variables and events that use the object,
objects on it (for a Layer) and the copies in derived elements follow. Hand-written code is not changed.

--file renames a file the element (or Global Content, with --global) references, and the file on disk.
<new name> may leave out the extension to keep the old one; the file stays in its folder.

The run fails (exit 1, changes not saved) when the target is unknown, the new name is invalid or taken, or a
question the rename asks is answered no. The message lists close matches or says which flag answers it.

With --json, ""edit"" holds {kind, element, oldName, newName, renamedFiles, updatedReferences,
changedElements}: kind is screen, entity, object or file; renamedFiles is [{from, to}] relative to the .csproj
folder; updatedReferences lists what was pointed at the new name; changedElements lists every screen and
entity the rename changed, by name after the rename.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity to rename, or that holds the object or file."),
            new("object", "name", "Rename this object in --element instead of the element."),
            new("file", "file", "Rename this file in --element or Global Content: its name as Glue shows it\n(Screens/GameScreen/Map.tmx) or just its file name (Map.tmx)."),
            new("global", null, "--file is in Global Content."),
            new("to", "new name", "The new name, without folders. Required."),
            new("overwrite", null, "Screens and entities: when code files with the new name already exist (Entities\\Blob.cs\nwith no Blob entity), replace them. Default: fail."),
            new("use-existing-file", null, "Files: when a file with the new name already exists on disk, point the file at it\nand leave the old one in place. Default: fail."),
        },
        Examples = new[]
        {
            ("rename MyGame --element Entities\\Enemy --to Slime", "Rename an entity: its files, the objects and lists that use it, its derived entities."),
            ("rename MyGame --element GameScreen --object SpriteInstance --to Background --json", "Rename an object in GameScreen, JSON output."),
            ("rename MyGame --element GameScreen --file Map.tmx --to Level1", "Rename a file GameScreen references (keeps .tmx) and the file on disk."),
            ("rename MyGame --global --file GlobalContent/Font.fnt --to Title.fnt", "Rename a Global Content file."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record Renamed(string Kind, string? Element, string OldName, string NewName,
        IReadOnlyList<RenamedFile> RenamedFiles, IReadOnlyList<string> UpdatedReferences, IReadOnlyList<string> ChangedElements);

    public sealed record RenamedFile(string From, string To);

    static string? ValidateArguments(CliContext context)
    {
        if (!context.Has("to"))
        {
            return "rename needs --to.";
        }
        if (context.Get("to")!.IndexOfAny(new[] { '/', '\\' }) != -1)
        {
            return "--to is the new name without folders; rename keeps the item in its folder.";
        }
        if (context.Has("file"))
        {
            if (context.Has("element") == context.Has("global"))
            {
                return "--file needs exactly one of --element or --global.";
            }
            if (context.Has("object"))
            {
                return "--object and --file cannot be combined.";
            }
            if (context.Has("overwrite"))
            {
                return "--overwrite is for screens and entities; for a file see --use-existing-file.";
            }
            return null;
        }
        if (!context.Has("element"))
        {
            return "rename needs --element (or --file with --global).";
        }
        if (context.Has("global"))
        {
            return "--global only goes with --file.";
        }
        if (context.Has("use-existing-file"))
        {
            return "--use-existing-file only goes with --file.";
        }
        if (context.Has("object") && context.Has("overwrite"))
        {
            return "--overwrite is for screens and entities, not objects.";
        }
        return null;
    }

    static async Task RunAsync(CliContext context)
    {
        if (context.Has("file"))
        {
            await RenameFileAsync(context);
            return;
        }

        var element = CliLookup.FindElement(context.Get("element")!, out var elementError);
        if (element == null)
        {
            context.Fail(elementError!);
            return;
        }
        GlueState.Self.CurrentElement = element;

        if (context.Has("object"))
        {
            await RenameObjectAsync(context, element);
        }
        else
        {
            await RenameElementAsync(context, element);
        }
    }

    static async Task RenameElementAsync(CliContext context, GlueElement element)
    {
        var oldName = element.Name;
        var newName = oldName.Substring(0, oldName.LastIndexOf('\\') + 1) + context.Get("to")!;
        if (newName == oldName)
        {
            context.Fail($"{oldName} already has that name.");
            return;
        }
        var verifier = Builder.Get<NameVerifier>();
        string whyInvalid;
        var isValid = element is ScreenSave screen
            ? verifier.IsScreenNameValid(newName, screen, out whyInvalid)
            : verifier.IsEntityNameValid(newName, (EntitySave)element, out whyInvalid);
        if (!isValid)
        {
            context.Fail($"Cannot rename {oldName} to {newName}: {whyInvalid}");
            return;
        }

        var kind = element is ScreenSave ? "screen" : "entity";
        var fileCommands = GlueCommands.Self.FileCommands;
        var jsonFileBefore = fileCommands.GetJsonFilePath(element);
        var before = SerializeElements();
        string? overwriteQuestion = null;

        RenameModifications? modifications;
        using (new CliDialogs(answerConfirm: (text, _, _) =>
        {
            if (!text.StartsWith(OverwriteQuestion, StringComparison.Ordinal))
            {
                return null;
            }
            overwriteQuestion = text;
            return context.Has("overwrite") ? DialogButton.Yes : DialogButton.No;
        }))
        {
            modifications = await GlueCommands.Self.GluxCommands.ElementCommands
                .RenameElementAndGetModifications(element, newName, showRenameWindow: false);
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        if (modifications == null)
        {
            context.Fail(overwriteQuestion != null
                ? $"Renaming {oldName} to {newName} would overwrite existing files:" + Environment.NewLine +
                  string.Join(Environment.NewLine, QuestionFileLines(overwriteQuestion).Select(line => "  " + line)) +
                  Environment.NewLine + "Pass --overwrite to replace them."
                : $"Glue did not rename {oldName}.");
            return;
        }

        var renamedFiles = new List<RenamedFile>();
        var jsonFileAfter = fileCommands.GetJsonFilePath(element);
        // Not checked on disk: the runner writes the renamed element's file after the command returns.
        if (jsonFileBefore != null && jsonFileAfter != null && jsonFileBefore != jsonFileAfter)
        {
            renamedFiles.Add(new RenamedFile(Relative(jsonFileBefore.FullPath), Relative(jsonFileAfter.FullPath)));
        }
        renamedFiles.AddRange(modifications.CodeFilesAffectedByRename
            .Where(change => change.OldFile != change.NewFile)
            .Select(change => new RenamedFile(Relative(change.OldFile.FullPath), Relative(change.NewFile.FullPath))));

        var updated = new List<string>();
        updated.AddRange(modifications.ElementsWithChangedBaseType.Select(item => $"base type of {item.Name}"));
        updated.AddRange(modifications.ObjectsWithChangedBaseEntity.Select(nos => $"type of {Describe(nos)}"));
        updated.AddRange(modifications.ObjectsWithChangedGenericBaseEntity.Select(nos => $"list type of {Describe(nos)}"));
        updated.AddRange(modifications.ChangedCollisionRelationships.Select(nos => $"collision relationship {Describe(nos)}"));
        updated.AddRange(modifications.ChangedNamedObjectVariables.Select(item => $"variable {Describe(item.NamedObjectSave)}.{item.VariableName}"));
        updated.AddRange(modifications.ChangedCustomVariables.Select(variable =>
            $"variable {ObjectFinder.Self.GetElementContaining(variable)?.Name}.{variable.Name}"));
        if (modifications.StartupScreenChange != null)
        {
            updated.Add("startup screen");
        }

        Report(context, new Renamed(kind, null, oldName, element.Name, renamedFiles, updated, ChangedElements(before)));
    }

    static async Task RenameObjectAsync(CliContext context, GlueElement element)
    {
        var objectName = context.Get("object")!;
        var allObjects = element.AllNamedObjects.ToList();
        var nos = allObjects.FirstOrDefault(item => item.InstanceName == objectName) ??
            allObjects.FirstOrDefault(item => string.Equals(item.InstanceName, objectName, StringComparison.OrdinalIgnoreCase));
        if (nos == null)
        {
            context.Fail($"{element.Name} has no object '{objectName}'." +
                (allObjects.Count == 0 ? " It has no objects." : CliLookup.DescribeCloseMatches(objectName, allObjects.Select(item => item.InstanceName))));
            return;
        }
        if (nos.DefinedByBase)
        {
            context.Fail($"{nos.InstanceName} in {element.Name} comes from its base {element.BaseElement}; rename it there.");
            return;
        }

        var oldName = nos.InstanceName;
        var newName = context.Get("to")!;
        if (newName == oldName)
        {
            context.Fail($"{oldName} already has that name.");
            return;
        }

        // The rename validates against the selected object, and skips it as "the object being renamed".
        GlueState.Self.CurrentNamedObjectSave = nos;
        if (!Builder.Get<NameVerifier>().IsNamedObjectNameValid(newName, nos, out var whyInvalid))
        {
            context.Fail($"Cannot rename {oldName} in {element.Name} to {newName}: {whyInvalid}");
            return;
        }
        // A SetByDerived base object of that name would turn this object into the base's copy.
        if (!string.IsNullOrEmpty(element.BaseElement) &&
            ObjectFinder.Self.GetElement(element.BaseElement)?.GetNamedObjectRecursively(newName) != null)
        {
            context.Fail($"Cannot rename {oldName} in {element.Name} to {newName}: the base {element.BaseElement} has an object with that name.");
            return;
        }

        // What ReactToNamedObjectChangedInstanceName points at the new name.
        var updated = new List<string>();
        updated.AddRange(element.CustomVariables.Where(item => item.SourceObject == oldName).Select(item => $"variable {element.Name}.{item.Name}"));
        updated.AddRange(element.Events.Where(item => item.SourceObject == oldName).Select(item => $"event {element.Name}.{item.EventName}"));
        if (nos.IsLayer)
        {
            updated.AddRange(element.AllNamedObjects.Where(item => item.LayerOn == oldName).Select(item => $"layer of {element.Name}.{item.InstanceName}"));
        }
        if (nos.ExposedInDerived || nos.SetByDerived)
        {
            updated.AddRange(ObjectFinder.Self.GetAllElementsThatInheritFrom(element)
                .Where(derived => derived.AllNamedObjects.Any(item => item.InstanceName == oldName && item.DefinedByBase))
                .Select(derived => $"object {derived.Name}.{oldName}"));
        }

        var before = SerializeElements();
        // The rename tells the user about each variable and event it repoints; those are reported in "edit".
        using (new CliDialogs(takeMessage: text =>
            text.StartsWith("Changing the variable ", StringComparison.Ordinal) ||
            text.StartsWith("Changing the Event ", StringComparison.Ordinal)))
        {
            await GlueCommands.Self.GluxCommands.RenameNamedObjectSave(nos, newName, performSaveAndGenerateCode: true, updateUi: false);
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        if (nos.InstanceName != newName)
        {
            context.Fail($"Glue did not rename {oldName} in {element.Name}.");
            return;
        }
        Report(context, new Renamed("object", element.Name, oldName, newName, Array.Empty<RenamedFile>(), updated, ChangedElements(before)));
    }

    static async Task RenameFileAsync(CliContext context)
    {
        GlueElement? container = null;
        List<ReferencedFileSave> files;
        if (context.Has("global"))
        {
            files = GlueState.Self.CurrentGlueProject.GlobalFiles;
        }
        else
        {
            container = CliLookup.FindElement(context.Get("element")!, out var elementError);
            if (container == null)
            {
                context.Fail(elementError!);
                return;
            }
            GlueState.Self.CurrentElement = container;
            files = container.ReferencedFiles;
        }
        var where = container?.Name ?? "Global Content";

        var fileName = context.Get("file")!.Replace('\\', '/');
        var rfs = files.FirstOrDefault(item => string.Equals(item.Name, fileName, StringComparison.OrdinalIgnoreCase));
        if (rfs == null)
        {
            var byFileName = files.Where(item => string.Equals(FileManager.RemovePath(item.Name), fileName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (byFileName.Count > 1)
            {
                context.Fail($"'{fileName}' matches more than one file in {where}; pass the full name: " + string.Join(", ", byFileName.Select(item => item.Name)));
                return;
            }
            rfs = byFileName.FirstOrDefault();
        }
        if (rfs == null)
        {
            context.Fail($"{where} has no file '{fileName}'." +
                (files.Count == 0 ? " It has no files." : CliLookup.DescribeCloseMatches(fileName, files.Select(item => item.Name))));
            return;
        }

        var oldName = rfs.Name;
        var newFileName = context.Get("to")!;
        if (Path.GetExtension(newFileName).Length == 0)
        {
            newFileName += Path.GetExtension(oldName);
        }
        var newName = FileManager.GetDirectory(oldName, RelativeType.Relative) + newFileName;
        if (newName == oldName)
        {
            context.Fail($"{oldName} already has that name.");
            return;
        }
        var instanceName = FileManager.RemovePath(FileManager.RemoveExtension(newName));
        if (!Builder.Get<NameVerifier>().IsReferencedFileNameValid(instanceName, rfs.GetAssetTypeInfo(), rfs, container, out var whyInvalid))
        {
            context.Fail($"Cannot rename {oldName} to {newName}: {whyInvalid}");
            return;
        }
        if (files.Any(item => item != rfs && string.Equals(item.Name, newName, StringComparison.OrdinalIgnoreCase)))
        {
            context.Fail($"Cannot rename {oldName} to {newName}: {where} already references {newName}.");
            return;
        }

        var oldPath = GlueCommands.Self.GetAbsoluteFileName(oldName, true);
        var newPath = GlueCommands.Self.GetAbsoluteFileName(newName, true);
        var useExisting = context.Has("use-existing-file");
        var oldFileExisted = File.Exists(oldPath);
        var askedAboutExisting = false;
        var before = SerializeElements();

        using (new CliDialogs(answerChoice: (text, options) =>
        {
            if (!text.StartsWith(ExistingFileQuestion, StringComparison.Ordinal))
            {
                return null;
            }
            askedAboutExisting = true;
            var label = useExisting ? UseExistingFileLabel : CancelRenameLabel;
            // An unknown label leaves the question to the runner, which reports it and fails the run.
            return options.FirstOrDefault(option => option.label == label).value;
        }))
        {
            GlueCommands.Self.FileCommands.RenameReferencedFileSave(rfs, newName);
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        if (rfs.Name != newName)
        {
            context.Fail(askedAboutExisting
                ? $"{Relative(newPath)} already exists. Pass --use-existing-file to point {oldName} at it (the old file stays)."
                : $"Glue did not rename {oldName}.");
            return;
        }

        var renamedFiles = new List<RenamedFile>();
        if (oldFileExisted && !askedAboutExisting)
        {
            renamedFiles.Add(new RenamedFile(Relative(oldPath), Relative(newPath)));
        }
        var changedElements = ChangedElements(before);
        var updated = changedElements.Where(name => name != container?.Name).Select(name => $"element {name}").ToList();
        Report(context, new Renamed("file", container?.Name, oldName, newName, renamedFiles, updated, changedElements));
    }

    /// <summary>The file lines of the overwrite question: everything between its first line and "Overwrite?".</summary>
    static IEnumerable<string> QuestionFileLines(string question) =>
        question.Split('\n').Skip(1).Select(line => line.Trim())
            .Where(line => line.Length > 0 && line != "Overwrite?")
            .Select(Relative);

    static string Describe(NamedObjectSave nos) => $"{ObjectFinder.Self.GetElementContaining(nos)?.Name}.{nos.InstanceName}";

    static string Relative(string absolutePath)
    {
        var root = GlueState.Self.CurrentMainProject?.Directory;
        var full = Path.GetFullPath(absolutePath);
        return (root == null ? full : Path.GetRelativePath(root, full)).Replace('\\', '/');
    }

    static Dictionary<GlueElement, string> SerializeElements()
    {
        var project = GlueState.Self.CurrentGlueProject;
        return project.Screens.Cast<GlueElement>().Concat(project.Entities)
            .ToDictionary(element => element, element => Newtonsoft.Json.JsonConvert.SerializeObject(element));
    }

    static List<string> ChangedElements(Dictionary<GlueElement, string> before) =>
        SerializeElements()
            .Where(pair => !before.TryGetValue(pair.Key, out var json) || json != pair.Value)
            .Select(pair => pair.Key.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    static void Report(CliContext context, Renamed result)
    {
        var what = result.Kind switch
        {
            "object" => $"object {result.OldName} in {result.Element}",
            "file" => $"file {result.OldName}" + (result.Element != null ? $" in {result.Element}" : " in Global Content"),
            _ => $"{result.Kind} {result.OldName}",
        };
        var lines = new List<string> { $"Renamed {what} to {result.NewName}." };
        lines.AddRange(result.RenamedFiles.Select(file => $"  moved {file.From} -> {file.To}"));
        lines.AddRange(result.UpdatedReferences.Select(item => $"  updated {item}"));
        if (result.ChangedElements.Count > 0)
        {
            lines.Add("  changed elements: " + string.Join(", ", result.ChangedElements));
        }
        context.Report(result, string.Join(Environment.NewLine, lines));
    }
}
