using System.ComponentModel;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using FlatRedBall.Glue.SetVariable;

namespace GlueCli.Commands;

/// <summary>
/// <c>set-file-property</c>: sets a property of a file (a <see cref="ReferencedFileSave"/>) in a screen, an entity or
/// Global Content, the way the editor's Properties grid does. The grid's own displayer
/// (<see cref="ReferencedFileSavePropertyGridDisplayer"/> plus the plugins' AdjustDisplayedReferencedFile) decides
/// which properties the file has in its container and sets the value, then the reaction the grid's change event
/// ends in runs (SetPropertyManager.ReactToPropertyChanged's file branch): ReferencedFileSaveSetPropertyManager,
/// the plugins, and the element's or Global Content's code generation.
/// </summary>
public static class SetFilePropertyCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "set-file-property",
        Summary = "Set a property of a file in a screen, an entity or Global Content (LoadedOnlyWhenReferenced, IsSharedStatic, RuntimeType, ...).",
        Usage = "set-file-property <project> (--element <element> | --global) --file <file> --property <name> --value <text> [--add-to-managers]",
        Description =
@"Sets the property the way the editor's Properties grid does for a selected file: same stored value, same
follow-up changes, plugin reactions and code generation. Then saves the .gluj and element files and
regenerates the code of the file's element (or GlobalContent.Generated.cs for a Global Content file).

<file> is the file's name in Glue (Screens/GameScreen/Background.png), its name in code when only one file
has it (Background), or its path on disk.

Only properties the editor shows for that file can be set, and which ones it shows depends on the file and
its container: LoadedAtRuntime, LoadedOnlyWhenReferenced, HasPublicProperty, IncludeDirectoryRelativeToContainer,
RuntimeType, UseContentPipeline, AddToManagers, SourceFile, BuildTool, AdditionalArguments, Summary,
ConditionalCompilationSymbols for most files; IsSharedStatic only in a screen; IsManuallyUpdated when the file's
type supports it and it is not in an entity; DestroyOnUnload for a Song outside an entity; CreatesDictionary,
IsDatabaseForLocalizing, UniformRowType and CsvDelimiter for a CSV; TreatAsCsv for a .txt. A file that is not
LoadedAtRuntime shows none of the loading properties. The error lists the ones that can be set. Name belongs
to rename.

<text> is converted to the property's type: true/false, an enum value name, a number, or text. A property
the editor picks from a list (RuntimeType, UniformRowType, BuildTool) must be one of the listed values.

IsSharedStatic true asks, for each object in the screen made from the file with AddToManagers false, whether
to set its AddToManagers to true; --add-to-managers answers yes, otherwise the objects are left as they are.

The run fails (exit 1, nothing saved) when the element, file or property is unknown or read-only, the text
does not convert, or Glue does not keep the value. The message lists close matches or the valid values.

With --json, ""edit"" holds {element, file, property, type, oldValue, newValue, answers, messages,
changedElements}: element is null for a Global Content file; answers lists [{question, answer}] for every
question Glue asked; messages lists what Glue reported while reacting; changedElements every screen and entity
the edit changed.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that has the file."),
            new("global", null, "The file is in Global Content."),
            new("file", "file", "The file: its name in Glue, in code, or its path on disk. Required."),
            new("property", "name", "Property name, as the Properties grid lists it (without spaces). Required."),
            new("value", "text", "New value as text. Required; pass \"\" for empty text."),
            new("add-to-managers", null, "IsSharedStatic true: set AddToManagers on the screen's objects made from the file.\nDefault: leave them."),
        },
        Examples = new[]
        {
            ("set-file-property MyGame --element Entities\\Enemy --file EnemyTexture --property LoadedOnlyWhenReferenced --value true", "Load an entity's png only when code first uses it."),
            ("set-file-property MyGame --element GameScreen --file Screens/GameScreen/Music.ogg --property RuntimeType --value Microsoft.Xna.Framework.Audio.SoundEffect --json", "Load an .ogg as a SoundEffect instead of a Song, JSON output."),
            ("set-file-property MyGame --global --file GlobalContent/EnemyInfo.csv --property CreatesDictionary --value false", "Make a Global Content CSV load as a List."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record SetFileProperty(string? Element, string File, string Property, string Type, object? OldValue, object? NewValue,
        IReadOnlyList<SetPropertyCommand.Answer> Answers, IReadOnlyList<string> Messages, IReadOnlyList<string> ChangedElements);

    /// <summary>ReferencedFileSaveSetPropertyManager, IsSharedStatic on a screen file used by an object with AddToManagers off.</summary>
    const string AddToManagersQuestion = "Shared files are not added to the engine, but since the object has its AddToManagers also set to false";

    /// <summary>ReferencedFileSaveSetPropertyManager, IsDatabaseForLocalizing when it changes whether the project localizes.</summary>
    const string LocalizationMessage = "Because of the change to the \"Is Database For Localizing\"";

    /// <summary>OpensWith's choice that opens a file picker for an application.</summary>
    const string NewApplication = "New Application...";

    static string? ValidateArguments(CliContext context)
    {
        if (context.Has("global") == context.Has("element"))
        {
            return "Pass --element <element> or --global, not both.";
        }
        foreach (var required in new[] { "file", "property", "value" })
        {
            if (!context.Has(required))
            {
                return $"set-file-property needs --{required}.";
            }
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
        var rfs = DeleteCommand.FindFile(context, element);
        if (rfs == null)
        {
            return;
        }
        // The grid shows the selected file; the reactions read the selection (and the element it is in).
        GlueState.Self.CurrentReferencedFileSave = rfs;
        var target = $"{rfs.Name} in {element?.Name ?? "Global Content"}";

        var propertyName = context.Get("property")!;
        if (string.Equals(propertyName, nameof(ReferencedFileSave.Name), StringComparison.OrdinalIgnoreCase))
        {
            var containerArgs = element != null ? $"--element {element.Name}" : "--global";
            context.Fail($"set-file-property does not rename. Use: rename <project> --file {rfs.Name} {containerArgs} --to <new name>");
            return;
        }

        var descriptors = GetGridProperties(rfs);
        var descriptor = descriptors.FirstOrDefault(item => item.Name == propertyName) ??
            descriptors.FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        var settable = descriptors.Where(item => !item.IsReadOnly && SetPropertyCommand.IsSupportedType(item.PropertyType) &&
                item.Name != nameof(ReferencedFileSave.Name))
            .Select(item => item.Name).ToList();
        if (descriptor == null)
        {
            var property = typeof(ReferencedFileSave).GetProperty(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            var close = CliLookup.CloseMatches(propertyName, settable);
            context.Fail((property != null
                    ? $"The editor does not show {property.Name} for {target}, so set-file-property does not set it there."
                    : $"{target} has no property '{propertyName}'.") +
                (close.Count > 0 ? " Did you mean: " + string.Join(", ", close) + "?" : "") +
                " Settable: " + string.Join(", ", settable.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)) + ".");
            return;
        }
        var name = descriptor.Name;
        if (descriptor.IsReadOnly)
        {
            context.Fail($"{name} of {target} is read-only in the editor." +
                (rfs.IsCreatedByWildcard ? " The file comes from a wildcard, so only IsCreatedByWildcard can change." : ""));
            return;
        }
        if (!SetPropertyCommand.IsSupportedType(descriptor.PropertyType))
        {
            context.Fail($"{name} of {target} is a {SetPropertyCommand.TypeName(descriptor.PropertyType)}; set-file-property only sets text, true/false, numbers and enum values.");
            return;
        }
        if (!SetPropertyCommand.TryConvert(context, descriptor, context.Get("value")!, $"{name} of {target}", out var value))
        {
            return;
        }
        if (name == nameof(ReferencedFileSave.OpensWith) && value is NewApplication)
        {
            context.Fail($"{NewApplication} opens a file picker. Pass the application's path as the value instead.");
            return;
        }

        var oldValue = descriptor.GetValue(null);
        var answers = new List<SetPropertyCommand.Answer>();
        var before = CliChanges.Snapshot();
        var dialogs = new CliDialogs(
            answerConfirm: (text, _, _) =>
            {
                if (!text.Contains(AddToManagersQuestion, StringComparison.Ordinal))
                {
                    return null;
                }
                var yes = context.Has("add-to-managers");
                lock (answers) { answers.Add(new SetPropertyCommand.Answer(SetPropertyCommand.FirstLine(text), yes ? "yes (--add-to-managers)" : "no")); }
                return yes ? DialogButton.Yes : DialogButton.No;
            },
            takeMessage: text => text.StartsWith(LocalizationMessage, StringComparison.Ordinal));
        using (dialogs)
        {
            descriptor.SetValue(null, value);

            // The reaction SetPropertyManager.ReactToPropertyChanged runs after either grid's change event.
            await Builder.Get<ReferencedFileSaveSetPropertyManager>().ReactToChangedReferencedFileAndGenerateCode(name, oldValue!);
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        var messages = dialogs.TakenMessages.ToList();
        var newValue = descriptor.GetValue(null);
        if (!SetPropertyCommand.SameValue(newValue, value))
        {
            context.Fail($"Glue did not set {name} of {target} to {SetPropertyCommand.Describe(value)}; it is {SetPropertyCommand.Describe(newValue)}.");
            return;
        }

        if (messages.Any(message => message.StartsWith(LocalizationMessage, StringComparison.Ordinal)))
        {
            // The editor asks the user to reopen the project, which regenerates everything; do that here.
            GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
            await TaskManager.Self.WaitForAllTasksFinished();
            messages.Add("Regenerated all code, since the change to IsDatabaseForLocalizing changes whether the project localizes.");
        }

        Report(context, new SetFileProperty(element?.Name, rfs.Name, name, SetPropertyCommand.TypeName(descriptor.PropertyType),
            oldValue, newValue, answers, messages, CliChanges.ChangedElements(before)));
    }

    /// <summary>
    /// The properties the editor's Properties grid shows for <paramref name="rfs"/>, built by the grid's own displayer
    /// and the plugins that add to it. Each descriptor sets its member as the grid does.
    /// </summary>
    static List<PropertyDescriptor> GetGridProperties(ReferencedFileSave rfs)
    {
        var displayer = new ReferencedFileSavePropertyGridDisplayer { Instance = rfs };
        PluginManager.AdjustDisplayedReferencedFile(rfs, displayer);
        displayer.RefreshOnTimer = false;
        return ((ICustomTypeDescriptor)displayer).GetProperties(Array.Empty<Attribute>()).Cast<PropertyDescriptor>().ToList();
    }

    static void Report(CliContext context, SetFileProperty result)
    {
        var lines = new List<string>
        {
            $"Set {result.File}.{result.Property} in {result.Element ?? "Global Content"} ({result.Type}): " +
            $"{SetPropertyCommand.Describe(result.OldValue)} -> {SetPropertyCommand.Describe(result.NewValue)}."
        };
        lines.AddRange(result.Answers.Select(answer => $"  asked: {answer.Question} -> {answer.Answered}"));
        lines.AddRange(result.Messages.Select(message => $"  glue: {SetPropertyCommand.FirstLine(message)}"));
        if (result.ChangedElements.Count > 0)
        {
            lines.Add("  changed elements: " + string.Join(", ", result.ChangedElements));
        }
        context.Report(result, string.Join(Environment.NewLine, lines));
    }
}
