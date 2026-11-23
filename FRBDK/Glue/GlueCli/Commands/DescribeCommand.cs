using System.Globalization;
using System.Text;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCli.Commands;

/// <summary>
/// <c>describe</c>: prints what the project, or one screen or entity, contains, with the names the edit
/// commands accept, so an agent does not have to read the .gluj/.glsj/.glej JSON. Read-only: it saves nothing.
/// </summary>
public static class DescribeCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "describe",
        Summary = "Show what the project or one screen or entity contains, with the names other commands accept.",
        Usage = "describe <project> [--element <element>] [--json]",
        Description =
@"Read-only. Loads the project and prints its contents. It writes nothing, as for check. The exit code is check's:
1 when the project has errors, with the description still printed.

Without --element: every screen and entity (name, folder, base), the startup screen, Global Content files,
the .gluj file version, and the Gum project if there is one.

With --element: that screen's or entity's base, its objects as a tree (lists and ShapeCollections with their
children), its custom variables, states and state categories, events, and referenced files. Each object
shows its type, where it comes from, and the variables set on it. Items a base element defines are marked
definedByBase; only what this element sets on them is shown. An unknown element exits 1 with close matches.

Names print as the edit commands take them: --element takes an element's name, --object an object's name,
--variable a variable's name.

With --json, ""edit"" holds the description.
Project: {kind: ""project"", fileVersion, startupScreen, gumProject (null without Gum),
  screens: [{name, folder, base}], entities: [{name, folder, base}], globalFiles: [{name, runtimeType}]}
Element: {kind: ""element"", name, elementType (""screen"" or ""entity""), base,
  objects: [{name, sourceType (FlatRedBallType, Entity, File, Gum), type, genericType, sourceFile and
    sourceName (File objects only), list (the containing list's name, or null), definedByBase, setByDerived, exposedInDerived,
    isDisabled, variables: [{name, value}], children: [object...]}],
  variables: [{name, type, defaultValue, sourceObject, sourceObjectProperty, definedByBase, setByDerived,
    isShared}],
  states: [state name...], stateCategories: [{name, states: [state name...]}],
  events: [{name, sourceObject, sourceObjectEvent}], files: [{name, runtimeType}]}
folder, base, type and the other string properties are null when not set. Values are numbers, booleans or
strings.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity to describe. Default: the whole project."),
        },
        Examples = new[]
        {
            ("describe MyGame", "List the screens, entities and Global Content files."),
            ("describe MyGame --element GameScreen", "Show GameScreen's objects, variables, states, events and files."),
            ("describe MyGame --element Entities\\Player --json", "Same for the Player entity, as JSON."),
        },
        IsEdit = false,
        Run = RunAsync,
    };

    public sealed record ProjectDescription(string Kind, int FileVersion, string? StartupScreen, string? GumProject,
        List<ElementSummary> Screens, List<ElementSummary> Entities, List<FileDescription> GlobalFiles);

    public sealed record ElementSummary(string Name, string? Folder, string? Base);

    public sealed record FileDescription(string Name, string? RuntimeType);

    public sealed record ElementDescription(string Kind, string Name, string ElementType, string? Base,
        List<ObjectDescription> Objects, List<VariableDescription> Variables, List<string> States,
        List<StateCategoryDescription> StateCategories, List<EventDescription> Events, List<FileDescription> Files);

    public sealed record ObjectDescription(string Name, string SourceType, string? Type, string? GenericType,
        string? SourceFile, string? SourceName, string? List, bool DefinedByBase, bool SetByDerived, bool ExposedInDerived,
        bool IsDisabled, List<SetValue> Variables, List<ObjectDescription> Children);

    public sealed record SetValue(string Name, object? Value);

    public sealed record VariableDescription(string Name, string? Type, object? DefaultValue, string? SourceObject,
        string? SourceObjectProperty, bool DefinedByBase, bool SetByDerived, bool IsShared);

    public sealed record StateCategoryDescription(string Name, List<string> States);

    public sealed record EventDescription(string Name, string? SourceObject, string? SourceObjectEvent);

    static Task RunAsync(CliContext context)
    {
        if (!context.Has("element"))
        {
            var project = DescribeProject();
            context.Report(project, ProjectText(project));
            return Task.CompletedTask;
        }

        var element = CliLookup.FindElement(context.Get("element")!, out var error);
        if (element == null)
        {
            context.Fail(error!);
            return Task.CompletedTask;
        }
        var description = DescribeElement(element);
        context.Report(description, ElementText(description));
        return Task.CompletedTask;
    }

    public static ProjectDescription DescribeProject()
    {
        var project = GlueState.Self.CurrentGlueProject;
        var gumProject = project.GetAllReferencedFiles().FirstOrDefault(file => GumFileExtensions.IsProject(file.Name));
        return new ProjectDescription(
            "project",
            project.FileVersion,
            NullIfEmpty(project.StartUpScreen),
            gumProject?.Name,
            project.Screens.Select(Summarize).ToList(),
            project.Entities.Select(Summarize).ToList(),
            project.GlobalFiles.Select(DescribeFile).ToList());
    }

    static ElementSummary Summarize(GlueElement element)
    {
        // Screens\Menus\Title: the folder is Menus, the part between the root and the name.
        var segments = element.Name.Split('\\');
        var folder = segments.Length > 2 ? string.Join("\\", segments.Skip(1).Take(segments.Length - 2)) : null;
        return new ElementSummary(element.Name, folder, NullIfEmpty(element.BaseElement));
    }

    static FileDescription DescribeFile(ReferencedFileSave file) => new(file.Name, NullIfEmpty(file.RuntimeType));

    public static ElementDescription DescribeElement(GlueElement element) => new(
        "element",
        element.Name,
        element is ScreenSave ? "screen" : "entity",
        NullIfEmpty(element.BaseElement),
        element.NamedObjects.Select(nos => DescribeObject(nos, null)).ToList(),
        element.CustomVariables.Select(variable => new VariableDescription(variable.Name, NullIfEmpty(variable.Type),
            ToJsonValue(variable.DefaultValue), NullIfEmpty(variable.SourceObject), NullIfEmpty(variable.SourceObjectProperty),
            variable.DefinedByBase, variable.SetByDerived, variable.IsShared)).ToList(),
        (element.States ?? new List<StateSave>()).Select(state => state.Name).ToList(),
        element.StateCategoryList.Select(category => new StateCategoryDescription(category.Name,
            category.States.Select(state => state.Name).ToList())).ToList(),
        element.Events.Select(item => new EventDescription(item.EventName, NullIfEmpty(item.SourceObject),
            NullIfEmpty(item.SourceObjectEvent))).ToList(),
        element.ReferencedFiles.Select(DescribeFile).ToList());

    static ObjectDescription DescribeObject(NamedObjectSave nos, string? list) => new(
        nos.InstanceName,
        nos.SourceType.ToString(),
        NullIfEmpty(nos.SourceClassType),
        NullIfEmpty(nos.SourceClassGenericType),
        // An object that was once from a file can keep a stale SourceFile; it means nothing for other types.
        nos.SourceType == SourceType.File ? NullIfEmpty(nos.SourceFile) : null,
        nos.SourceType == SourceType.File ? NullIfEmpty(nos.SourceName) : null,
        list,
        nos.DefinedByBase,
        nos.SetByDerived,
        nos.ExposedInDerived,
        nos.IsDisabled,
        nos.InstructionSaves.Select(instruction => new SetValue(instruction.Member, ToJsonValue(instruction.Value))).ToList(),
        nos.ContainedObjects.Select(child => DescribeObject(child, nos.InstanceName)).ToList());

    static string ProjectText(ProjectDescription project)
    {
        var text = new StringBuilder();
        text.AppendLine($"File version {project.FileVersion}, startup screen: {project.StartupScreen ?? "(none)"}, " +
            $"Gum: {project.GumProject ?? "none"}.");
        AppendElements("Screens", project.Screens);
        AppendElements("Entities", project.Entities);
        AppendFiles(text, "Global Content", project.GlobalFiles);
        return text.ToString().TrimEnd();

        void AppendElements(string title, List<ElementSummary> elements)
        {
            text.AppendLine($"{title} ({elements.Count}):");
            foreach (var element in elements)
            {
                var startup = element.Name == project.StartupScreen ? "  (startup)" : "";
                var baseText = element.Base != null ? $"  : {element.Base}" : "";
                text.AppendLine($"  {element.Name}{baseText}{startup}");
            }
        }
    }

    static string ElementText(ElementDescription element)
    {
        var text = new StringBuilder();
        text.AppendLine($"{element.Name} ({element.ElementType}{(element.Base != null ? ", base " + element.Base : "")})");

        text.AppendLine($"Objects ({CountObjects(element.Objects)}):");
        foreach (var nos in element.Objects)
        {
            AppendObject(nos, 1);
        }

        text.AppendLine($"Variables ({element.Variables.Count}):");
        foreach (var variable in element.Variables)
        {
            var source = variable.SourceObject != null ? $"  <- {variable.SourceObject}.{variable.SourceObjectProperty}" : "";
            text.AppendLine($"  {variable.Name} {variable.Type} = {FormatValue(variable.DefaultValue)}{source}" +
                Flags(variable.DefinedByBase, variable.SetByDerived, false, false) + (variable.IsShared ? " [static]" : ""));
        }

        if (element.States.Count > 0)
        {
            text.AppendLine($"States: {string.Join(", ", element.States)}");
        }
        if (element.StateCategories.Count > 0)
        {
            text.AppendLine($"State categories ({element.StateCategories.Count}):");
            foreach (var category in element.StateCategories)
            {
                text.AppendLine($"  {category.Name}: {string.Join(", ", category.States)}");
            }
        }
        if (element.Events.Count > 0)
        {
            text.AppendLine($"Events ({element.Events.Count}):");
            foreach (var item in element.Events)
            {
                var source = item.SourceObject != null ? $"  <- {item.SourceObject}.{item.SourceObjectEvent}" : "";
                text.AppendLine($"  {item.Name}{source}");
            }
        }
        AppendFiles(text, "Files", element.Files);
        return text.ToString().TrimEnd();

        void AppendObject(ObjectDescription nos, int depth)
        {
            var type = nos.SourceType == "File"
                ? $"from {nos.SourceFile}: {nos.SourceName}"
                : nos.GenericType != null ? $"{ShortType(nos.Type)}<{nos.GenericType}>" : ShortType(nos.Type);
            var variables = nos.Variables.Count > 0
                ? "  " + string.Join(" ", nos.Variables.Select(variable => $"{variable.Name}={FormatValue(variable.Value)}"))
                : "";
            text.AppendLine($"{new string(' ', depth * 2)}{nos.Name}  {type}{variables}" +
                Flags(nos.DefinedByBase, nos.SetByDerived, nos.ExposedInDerived, nos.IsDisabled));
            foreach (var child in nos.Children)
            {
                AppendObject(child, depth + 1);
            }
        }
    }

    static void AppendFiles(StringBuilder text, string title, List<FileDescription> files)
    {
        text.AppendLine($"{title} ({files.Count}):");
        foreach (var file in files)
        {
            text.AppendLine($"  {file.Name}{(file.RuntimeType != null ? "  " + file.RuntimeType : "")}");
        }
    }

    static int CountObjects(List<ObjectDescription> objects) => objects.Sum(nos => 1 + CountObjects(nos.Children));

    static string Flags(bool definedByBase, bool setByDerived, bool exposedInDerived, bool isDisabled)
    {
        var flags = new List<string>();
        if (definedByBase) flags.Add("definedByBase");
        if (setByDerived) flags.Add("setByDerived");
        if (exposedInDerived) flags.Add("exposedInDerived");
        if (isDisabled) flags.Add("disabled");
        return flags.Count == 0 ? "" : " [" + string.Join(", ", flags) + "]";
    }

    /// <summary>
    /// <c>FlatRedBall.Math.Geometry.AxisAlignedRectangle</c> as <c>AxisAlignedRectangle</c>, and
    /// <c>FlatRedBall.Math.PositionedObjectList&lt;T&gt;</c> as <c>PositionedObjectList</c>, for the text output.
    /// Entity names (<c>Entities\Enemy</c>) and the generic arguments of collision relationships stay whole.
    /// </summary>
    static string ShortType(string? type)
    {
        if (type == null)
        {
            return "(no type)";
        }
        var genericStart = type.IndexOf('<');
        var head = genericStart == -1 ? type : type.Substring(0, genericStart);
        var tail = genericStart == -1 ? "" : type.Substring(genericStart);
        if (tail == "<T>")
        {
            tail = "";
        }
        return head.Substring(head.LastIndexOf('.') + 1) + tail;
    }

    /// <summary>
    /// A value as System.Text.Json can write it: numbers, booleans and strings as they are, anything else
    /// (a Newtonsoft JToken, a Color) as its text.
    /// </summary>
    static object? ToJsonValue(object? value) => value switch
    {
        null => null,
        string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => value,
        Enum => value.ToString(),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture),
    };

    static string FormatValue(object? value) => value switch
    {
        null => "(unset)",
        string text => $"\"{text}\"",
        bool flag => flag ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    static string? NullIfEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;
}
