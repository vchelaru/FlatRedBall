using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using FlatRedBall.Glue.ViewModels;

namespace GlueCli.Commands;

/// <summary>
/// <c>add-object</c>: adds a NamedObjectSave to a screen or entity through
/// <see cref="FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.IGluxCommands.AddNewNamedObjectToAsync"/>,
/// the call the editor's Add Object dialog ends in, so plugins react and code generates as in the editor.
/// The dialog's own checks (name, recursion, list placement) run here first, since this path has no dialog.
/// </summary>
public static class AddObjectCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "add-object",
        Summary = "Add an object (a Sprite, a Circle, an entity instance, a list, an object from a file) to a screen or entity.",
        Usage = "add-object <project> --element <element> (--type <type> | --source-file <file> --source-name <name>) [options]",
        Description =
@"Adds the object the way the editor's Add Object dialog does: same default name, same list placement, same
code generation and plugin reactions. Then saves the .gluj and element files and regenerates the element's code.

<element> is a screen or entity: GameScreen, Screens\GameScreen, Entities\Enemy (either slash).
<type> is a FlatRedBall type by its Glue name or class name (Sprite, Circle, AxisAlignedRectangle, Text,
Layer, TileShapeCollection, FlatRedBall.Sprite, ...), a Gum type, or an entity (Enemy, Entities\Enemy).

Without --list, an entity instance goes into the element's list of that entity type (or of a base type) if
one exists, as in the editor. Everything else is added at the top level.

The run fails (exit 1, changes not saved) when the name is taken or invalid, the type or element is unknown,
the element would contain itself, or the list cannot hold the type. The message lists close matches.

With --json, ""edit"" holds the added object: {element, name, sourceType, type, genericType, sourceFile,
sourceName, list}, where list is the list it went into or null.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity to add to. Required."),
            new("type", "type", "What to add. Required unless --source-file is given."),
            new("name", "name", "Instance name. Default: Glue's default (SpriteInstance, EnemyInstance, EnemyList...),\nnumbered if taken."),
            new("list", "list", "Add into this list (or ShapeCollection) in the element instead of the default placement."),
            new("no-list", null, "Add at the top level even if a list of this type exists."),
            new("as-list", null, "Add a PositionedObjectList of <type> instead of a single object."),
            new("source-file", "file", "Add an object from a file the element references (its name as Glue shows it, for\nexample Screens/GameScreen/Map.tmx, or just Map.tmx). Use with --source-name."),
            new("source-name", "name", "The object in --source-file, as the editor lists it: \"Entire File (TileMap)\",\n\"SpriteInstance (Sprite)\"."),
        },
        Examples = new[]
        {
            ("add-object MyGame --element GameScreen --type Sprite", "Add a Sprite named SpriteInstance to GameScreen."),
            ("add-object MyGame/MyGame.gluj --element Entities\\Player --type Circle --name HitCircle --json", "Add a Circle named HitCircle to the Player entity, JSON output."),
            ("add-object MyGame --element GameScreen --type Enemy", "Add an Enemy instance; it goes into GameScreen's EnemyList if there is one."),
            ("add-object MyGame --element GameScreen --type Enemy --as-list", "Add a list of Enemy (named EnemyList)."),
            ("add-object MyGame --element GameScreen --source-file Map.tmx --source-name \"Entire File (LayeredTileMap)\"", "Add the whole map file as an object."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record AddedObject(string Element, string Name, string SourceType, string? Type, string? GenericType,
        string? SourceFile, string? SourceName, string? List);

    static string? ValidateArguments(CliContext context)
    {
        if (!context.Has("element"))
        {
            return "add-object needs --element.";
        }
        var hasFile = context.Has("source-file");
        if (context.Has("type") == hasFile)
        {
            return "add-object needs exactly one of --type or --source-file.";
        }
        if (hasFile != context.Has("source-name"))
        {
            return "--source-file and --source-name go together.";
        }
        if (new[] { "list", "no-list", "as-list" }.Count(context.Has) > 1)
        {
            return "--list, --no-list and --as-list cannot be combined.";
        }
        if (hasFile && context.Has("as-list"))
        {
            return "--as-list cannot be used with --source-file.";
        }
        return null;
    }

    static async Task RunAsync(CliContext context)
    {
        var element = CliLookup.FindElement(context.Get("element")!, out var elementError);
        if (element == null)
        {
            context.Fail(elementError!);
            return;
        }

        // NameVerifier and AddNewNamedObjectToAsync's DefinedByBase check read the current element, and a
        // selected object would make NameVerifier skip it as "the object being renamed".
        GlueState.Self.CurrentElement = element;

        var viewModel = new AddObjectViewModel();
        // Order matters: each type setter recomputes the default name, so the element comes first and an
        // explicit name last.
        viewModel.ForcedElementToAddTo = element;

        var typeSet = context.Has("source-file")
            ? SetSourceFile(context, element, viewModel)
            : SetType(context, viewModel);
        if (!typeSet)
        {
            return;
        }

        if (context.Has("name"))
        {
            viewModel.ObjectName = context.Get("name");
        }

        var name = viewModel.ObjectName;
        if (string.IsNullOrEmpty(name))
        {
            context.Fail("Glue has no default name for this object; pass --name.");
            return;
        }
        if (!Builder.Get<NameVerifier>().IsNamedObjectNameValid(name, null, out var whyNameIsInvalid))
        {
            context.Fail($"'{name}' is not a valid name in {element.Name}: {whyNameIsInvalid}");
            return;
        }
        if (viewModel.SourceType == SourceType.Entity && !RecursionManager.Self.CanContainInstanceOf(element, viewModel.SourceClassType))
        {
            context.Fail($"{element.Name} cannot contain an instance of {viewModel.SourceClassType}: it is that entity or derives from it.");
            return;
        }

        var list = ChooseList(context, element, viewModel);
        if (context.HasFailed)
        {
            return;
        }

        var nos = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(viewModel, element, list, selectNewNos: false);

        var added = new AddedObject(
            Element: element.Name,
            Name: nos.InstanceName,
            SourceType: nos.SourceType.ToString(),
            Type: nos.SourceType == SourceType.File ? null : nos.SourceClassType,
            GenericType: string.IsNullOrEmpty(nos.SourceClassGenericType) ? null : nos.SourceClassGenericType,
            SourceFile: nos.SourceType == SourceType.File ? nos.SourceFile : null,
            SourceName: nos.SourceType == SourceType.File ? nos.SourceName : null,
            List: list?.InstanceName);

        var description = added.SourceFile != null
            ? $"{added.SourceName} from {added.SourceFile}"
            : added.GenericType != null ? $"{added.Type} of {added.GenericType}" : added.Type;
        context.Report(added, $"Added {added.Name} ({description}) to {added.Element}" +
            (added.List != null ? $", in list {added.List}." : "."));
    }

    static bool SetSourceFile(CliContext context, GlueElement element, AddObjectViewModel viewModel)
    {
        var fileArgument = context.Get("source-file")!.Replace('\\', '/');
        var files = element.ReferencedFiles;
        var rfs = files.FirstOrDefault(file => string.Equals(file.Name.Replace('\\', '/'), fileArgument, StringComparison.OrdinalIgnoreCase));
        if (rfs == null)
        {
            var byFileName = files.Where(file => string.Equals(CliLookup.StripFolders(file.Name), fileArgument, StringComparison.OrdinalIgnoreCase)).ToList();
            if (byFileName.Count > 1)
            {
                context.Fail($"'{fileArgument}' matches more than one file in {element.Name}: " + string.Join(", ", byFileName.Select(file => file.Name)));
                return false;
            }
            rfs = byFileName.FirstOrDefault();
        }
        if (rfs == null)
        {
            context.Fail($"{element.Name} has no file '{fileArgument}'." +
                (files.Count == 0 ? " It references no files." : CliLookup.DescribeCloseMatches(fileArgument, files.Select(file => file.Name))));
            return false;
        }

        viewModel.SourceType = SourceType.File;
        viewModel.SourceFile = rfs;

        var available = new List<string>();
        AvailableNameablesStringConverter.FillListWithAvailableObjects(rfs.Name, available);
        var sourceName = context.Get("source-name")!;
        var match = available.FirstOrDefault(item => item == sourceName) ??
            available.FirstOrDefault(item => string.Equals(item, sourceName, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            context.Fail($"{rfs.Name} has no object '{sourceName}'." +
                (available.Count == 0 ? " Glue lists no objects in it." : " Available: " + string.Join(", ", available.Select(item => $"\"{item}\"")) + "."));
            return false;
        }
        viewModel.SourceNameInFile = match;
        return true;
    }

    static bool SetType(CliContext context, AddObjectViewModel viewModel)
    {
        var typeArgument = context.Get("type")!;
        var entity = ResolveType(typeArgument, out var ati, out var typeError);
        if (entity == null && ati == null)
        {
            context.Fail(typeError!);
            return false;
        }

        if (context.Has("as-list"))
        {
            var genericType = entity?.Name ?? ati!.QualifiedRuntimeTypeName.QualifiedType;
            var listTypes = new AvailableClassGenericTypeConverter().GetAvailableValues(false);
            if (!listTypes.Contains(genericType))
            {
                context.Fail($"{genericType} cannot be the type of a list. Lists hold entities or positioned objects such as Sprite, Circle, Text.");
                return false;
            }
            viewModel.SourceType = SourceType.FlatRedBallType;
            viewModel.SelectedAti = AvailableAssetTypes.CommonAtis.PositionedObjectList;
            viewModel.SourceClassGenericType = genericType;
        }
        else if (entity != null)
        {
            viewModel.SourceType = SourceType.Entity;
            viewModel.SelectedEntitySave = entity;
        }
        else
        {
            viewModel.SourceType = IsGum(ati!) ? SourceType.Gum : SourceType.FlatRedBallType;
            viewModel.SelectedAti = ati;
        }
        return true;
    }

    /// <summary>
    /// The entity or asset type <paramref name="typeArgument"/> names. Exact names win over case-insensitive
    /// and unqualified ones; a name that is both an entity and a type is refused so the caller qualifies it.
    /// </summary>
    static EntitySave? ResolveType(string typeArgument, out AssetTypeInfo? ati, out string? error)
    {
        ati = null;
        error = null;
        var normalized = typeArgument.Replace('/', '\\');
        var entities = GlueState.Self.CurrentGlueProject.Entities;
        var objectTypes = AvailableAssetTypes.Self.AllAssetTypes.Where(item => item.CanBeObject && !IsGumScreen(item)).ToList();

        var exactEntity = entities.FirstOrDefault(item => item.Name == normalized);
        var exactTypes = objectTypes.Where(item => item.FriendlyName == typeArgument ||
            item.QualifiedRuntimeTypeName.QualifiedType == typeArgument).ToList();

        List<EntitySave> entityMatches;
        List<AssetTypeInfo> typeMatches;
        if (exactEntity != null || exactTypes.Count > 0)
        {
            entityMatches = exactEntity != null ? new List<EntitySave> { exactEntity } : new List<EntitySave>();
            typeMatches = exactTypes;
        }
        else
        {
            entityMatches = entities.Where(item => string.Equals(item.Name, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(CliLookup.StripFolders(item.Name), normalized, StringComparison.OrdinalIgnoreCase))
                .ToList();
            typeMatches = objectTypes.Where(item =>
                    string.Equals(item.FriendlyName, typeArgument, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.QualifiedRuntimeTypeName.QualifiedType, typeArgument, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.RuntimeTypeName, typeArgument, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Unqualify(item.QualifiedRuntimeTypeName.QualifiedType), typeArgument, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var candidateCount = entityMatches.Count + typeMatches.Count;
        if (candidateCount == 1)
        {
            ati = typeMatches.FirstOrDefault();
            return entityMatches.FirstOrDefault();
        }
        if (candidateCount > 1)
        {
            error = $"'{typeArgument}' could mean more than one type; pass one of: " +
                string.Join(", ", entityMatches.Select(item => item.Name)
                    .Concat(typeMatches.Select(item => item.QualifiedRuntimeTypeName.QualifiedType ?? item.FriendlyName)));
            return null;
        }

        error = $"No entity or object type named '{typeArgument}'." + CliLookup.DescribeCloseMatches(typeArgument,
            entities.Select(item => item.Name).Concat(objectTypes.Select(item => item.FriendlyName)));
        return null;
    }

    /// <summary>
    /// The list the object goes into: --list if given (checked against what the editor would allow), none for
    /// --no-list and --as-list, otherwise the editor's default (<see cref="ObjectFinder.GetDefaultListToContain(string, GlueElement)"/>,
    /// called with the same argument <c>AddNewNamedObjectToSelectedElementAsync</c> passes).
    /// </summary>
    static NamedObjectSave? ChooseList(CliContext context, GlueElement element, AddObjectViewModel viewModel)
    {
        if (context.Has("no-list") || context.Has("as-list"))
        {
            return null;
        }
        if (!context.Has("list"))
        {
            return ObjectFinder.Self.GetDefaultListToContain(viewModel.SourceClassType, element);
        }

        var listName = context.Get("list")!;
        var compatible = CompatibleLists(element, viewModel);
        var list = compatible.FirstOrDefault(item => item.InstanceName == listName) ??
            compatible.FirstOrDefault(item => string.Equals(item.InstanceName, listName, StringComparison.OrdinalIgnoreCase));
        if (list == null)
        {
            var exists = element.NamedObjects.Any(item => string.Equals(item.InstanceName, listName, StringComparison.OrdinalIgnoreCase));
            context.Fail((exists
                    ? $"{listName} in {element.Name} cannot hold a {viewModel.SourceClassType}."
                    : $"{element.Name} has no list named '{listName}'.") +
                (compatible.Count == 0
                    ? " No list in it can hold this type."
                    : " Lists that can: " + string.Join(", ", compatible.Select(item => item.InstanceName)) + "."));
        }
        return list;
    }

    /// <summary>
    /// Top-level lists and ShapeCollections in <paramref name="element"/> that can hold the object: the lists
    /// <see cref="ObjectFinder.GetPossibleListsToContain(string, GlueElement)"/> finds (an entity's own or base type),
    /// lists of the asset type, and ShapeCollections for shapes, as <c>AddNewNamedObjectToSelectedElementAsync</c>
    /// accepts for the selected list.
    /// </summary>
    static List<NamedObjectSave> CompatibleLists(GlueElement element, AddObjectViewModel viewModel)
    {
        if (viewModel.SourceType == SourceType.File)
        {
            return new List<NamedObjectSave>();
        }
        var lists = ObjectFinder.Self.GetPossibleListsToContain(viewModel.SourceClassType, element);
        var ati = viewModel.SelectedAti;
        if (ati != null)
        {
            lists.AddRange(element.NamedObjects.Where(item => item.IsList &&
                (item.SourceClassGenericType == ati.FriendlyName || item.SourceClassGenericType == ati.QualifiedRuntimeTypeName.QualifiedType)));

            var common = AvailableAssetTypes.CommonAtis;
            var isShape = ati == common.AxisAlignedRectangle || ati == common.CapsulePolygon || ati == common.Circle ||
                ati == common.Polygon || ati == common.Line;
            if (isShape)
            {
                lists.AddRange(element.NamedObjects.Where(item => item.GetAssetTypeInfo() == common.ShapeCollection));
            }
        }
        return lists.Distinct().ToList();
    }

    static bool IsGum(AssetTypeInfo ati) => PluginManager.CallPluginMethod("Gum Plugin", "IsAssetTypeInfoGum", ati) is true;

    // Compared by name so GlueCli needs no reference to Gum's data types. The editor leaves Gum screens out
    // of Add Object too.
    static bool IsGumScreen(AssetTypeInfo ati) => ati.Tag?.GetType().FullName == "Gum.DataTypes.ScreenSave";

    static string? Unqualify(string? qualified)
    {
        if (qualified == null)
        {
            return null;
        }
        var genericStart = qualified.IndexOf('<');
        var withoutGeneric = genericStart == -1 ? qualified : qualified.Substring(0, genericStart);
        return withoutGeneric.Substring(withoutGeneric.LastIndexOf('.') + 1);
    }
}
