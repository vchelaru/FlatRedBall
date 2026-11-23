using System.IO;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using FlatRedBall.IO;

namespace GlueCli.Commands;

/// <summary>
/// <c>add-file</c>: adds an existing file as a ReferencedFileSave to a screen, an entity or Global Content
/// through <see cref="FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.IGluxCommands.CreateReferencedFileSaveForExistingFileAsync"/>,
/// the call dragging a file onto the tree and Add Existing File end in (through <c>AddExistingFileManager.AddSingleFile</c>),
/// so the copy, the .csproj entry, CSV classes, code generation and plugin reactions run as in the editor.
/// What those paths would ask or decide silently (an unknown extension, a copy into the content folder, a
/// file already referenced, a name collision) is checked here first, since this path has no dialog.
/// </summary>
public static class AddFileCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "add-file",
        Summary = "Add an existing file (png, achx, tmx, csv, wav, ...) to a screen, an entity or Global Content.",
        Usage = "add-file <project> (--element <element> | --global) --file <path> [--copy] [--allow-unknown-type]",
        Description =
@"Adds the file the way dragging it onto the element in the editor's tree does: same name, same .csproj entry,
same CSV data class, same plugin reactions (Tiled, animation chains, Gum, CSV). Then saves the .gluj and element
files and regenerates code.

<element> is a screen or entity: GameScreen, Screens\GameScreen, Entities\Enemy (either slash).
<path> is the file to add. A relative path is tried against the current folder, then the project's content
folder. The file keeps its name; Glue names it by its path relative to the content folder
(Screens/GameScreen/Background.png), and code uses its name without extension (Background).

A file outside the content folder must be copied in: pass --copy and it goes into the element's content
folder (Content/Screens/GameScreen/, Content/GlobalContent/), with the files it references (an .achx's
images, a .tmx's tilesets) copied next to it, as the editor does.

The run fails (exit 1, nothing saved) when the file does not exist, is outside the content folder without
--copy, would be copied over a file already there, is already referenced by the element (or Global
Content), or its name is taken or invalid in Glue. A file type Glue does not know (no code is generated for
it) fails unless --allow-unknown-type is given. Creating new files (the editor's New File dialog) and files
handled by a build tool are not supported yet.

With --json, ""edit"" holds the added file: {element, name, filePath, runtimeType, csvRowType, copiedFrom},
where element is null for Global Content, name is Glue's name for the file, filePath is absolute, runtimeType
is the type the file loads as (null for CSVs and unknown types), csvRowType is the row class of a CSV, and
copiedFrom is the original path when --copy copied it.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity to add the file to."),
            new("global", null, "Add the file to Global Content instead of an element."),
            new("file", "path", "The file to add. Required."),
            new("copy", null, "Copy a file outside the content folder into the element's content folder first."),
            new("allow-unknown-type", null, "Add a file whose extension Glue does not recognize (the editor's \"add it anyway?\").\nGlue generates no code for it."),
        },
        Examples = new[]
        {
            ("add-file MyGame --element GameScreen --file MyGame/Content/Screens/GameScreen/Background.png", "Add a png already in the content folder to GameScreen."),
            ("add-file MyGame --global --file C:/Art/EnemyInfo.csv --copy --json", "Copy a CSV into Content/GlobalContent and add it to Global Content, JSON output."),
            ("add-file MyGame --element Entities\\Player --file Content/Entities/Player/PlayerAnimations.achx", "Add an animation chain file to the Player entity."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record AddedFile(string? Element, string Name, string FilePath, string? RuntimeType, string? CsvRowType, string? CopiedFrom);

    static string? ValidateArguments(CliContext context)
    {
        if (context.Has("element") == context.Has("global"))
        {
            return "add-file needs exactly one of --element or --global.";
        }
        if (!context.Has("file"))
        {
            return "add-file needs --file.";
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
        // The editor adds to the element selected in the tree; nothing selected means Global Content.
        GlueState.Self.CurrentElement = element;
        var containerName = element?.Name ?? "Global Content";

        var contentFolder = GlueState.Self.ContentDirectory;
        var source = ResolveFile(context.Get("file")!, contentFolder);
        if (source == null)
        {
            context.Fail($"{context.Get("file")} does not exist (tried the current folder and {contentFolder}).");
            return;
        }

        var extension = source.Extension;
        var assetTypes = AvailableAssetTypes.Self;
        if (BuildToolAssociationManager.Self.GetIfIsBuiltFile(source.FullPath))
        {
            // AddExistingFileManager asks which build tool to use in a window; there is no flag for that yet.
            context.Fail($"Files with the extension .{extension} go through a build tool in this Glue's settings, which add-file does not support yet.");
            return;
        }
        var isUnknownType = assetTypes.GetAssetTypeFromExtension(extension) == null && extension != "csv" &&
            !assetTypes.AdditionalExtensionsToTreatAsAssets.Contains(extension);
        if (isUnknownType && !context.Has("allow-unknown-type"))
        {
            context.Fail($"Glue does not recognize the extension .{extension}, so it would generate no code for the file. " +
                "Pass --allow-unknown-type to add it anyway.");
            return;
        }

        var existing = (element != null ? element.GetAllReferencedFileSavesRecursively() : GlueState.Self.CurrentGlueProject.GlobalFiles)
            .FirstOrDefault(rfs => GlueCommands.Self.FileCommands.GetFilePath(rfs) == source);
        if (existing != null)
        {
            context.Fail($"{containerName} already references {existing.Name}.");
            return;
        }

        var isInContentFolder = FileManager.IsRelativeTo(source.FullPath, contentFolder);
        FilePath? destination = null;
        if (!isInContentFolder)
        {
            // Where CreateReferencedFileSaveForExistingFile copies it.
            destination = new FilePath(ElementCommands.GetFullPathContentDirectory(element, null) + source.NoPath);
            if (!context.Has("copy"))
            {
                context.Fail($"{source.FullPath} is outside the content folder {contentFolder}. " +
                    $"Pass --copy to copy it to {destination.FullPath} and add the copy.");
                return;
            }
            if (destination.Exists())
            {
                context.Fail($"{destination.FullPath} already exists, and the copy would replace it. " +
                    "Add that file instead (--file with its path), or move one of them.");
                return;
            }
        }

        var ati = assetTypes.GetAssetTypeFromExtension(extension);
        if (!Builder.Get<NameVerifier>().IsReferencedFileNameValid(source.NoPathNoExtension, ati, null, element, out var whyNameIsInvalid))
        {
            context.Fail($"{source.NoPath} cannot be added to {containerName}: {whyNameIsInvalid}");
            return;
        }

        if (isUnknownType)
        {
            // The same as answering "yes" to ElementCommands.CheckAndWarnAboutUnknownFileTypes' question: an
            // extension in this list is treated as already confirmed, and ProjectLoader rebuilds the list from
            // the project's files on every load.
            assetTypes.AdditionalExtensionsToTreatAsAssets.Add(extension);
        }

        var response = await GlueCommands.Self.GluxCommands.CreateReferencedFileSaveForExistingFileAsync(element, source);
        if (!response.Succeeded)
        {
            context.Fail($"Glue did not add {source.NoPath}: {response.Message ?? "no reason given."}");
            return;
        }

        var added = response.Data;
        var isCsv = added.IsCsvOrTreatedAsCsv;
        var addedFile = new AddedFile(
            Element: element?.Name,
            Name: added.Name,
            FilePath: GlueCommands.Self.FileCommands.GetFilePath(added).FullPath,
            RuntimeType: isCsv ? null : added.GetAssetTypeInfo()?.QualifiedRuntimeTypeName.QualifiedType,
            CsvRowType: isCsv ? added.GetTypeForCsvFile() : null,
            CopiedFrom: destination != null ? source.FullPath : null);

        var typeText = addedFile.RuntimeType ?? (addedFile.CsvRowType != null ? $"CSV of {addedFile.CsvRowType}" : "unknown type, no code generated");
        context.Report(addedFile, $"Added {addedFile.Name} ({typeText}) to {containerName}" +
            (addedFile.CopiedFrom != null ? $", copied from {addedFile.CopiedFrom}." : "."));
    }

    static FilePath? ResolveFile(string argument, string contentFolder)
    {
        var roots = Path.IsPathRooted(argument) ? new[] { "" } : new[] { Directory.GetCurrentDirectory(), contentFolder };
        foreach (var root in roots)
        {
            var candidate = Path.GetFullPath(Path.Combine(root, argument));
            if (File.Exists(candidate))
            {
                return new FilePath(CliLookup.ToLongPath(candidate));
            }
        }
        return null;
    }
}
