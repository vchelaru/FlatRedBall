using System.IO;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using GlueFormsCore.ViewModels;

namespace GlueCli.Commands;

/// <summary>
/// <c>add-event</c>: adds an event to a screen or entity through
/// <see cref="FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.IElementCommands.AddEventToElement(AddEventViewModel, GlueElement)"/>,
/// the call the editor's Add Event window ends in. The window's three choices map to --expose/--variable,
/// --object, and --custom, with the window's lists (<see cref="ExposedEventManager"/>,
/// <see cref="AvailableDelegateTypeConverter"/>) as the valid values. The name checks the window leaves to that
/// call, plus a duplicate check it does not make, run here before anything changes.
/// </summary>
public static class AddEventCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "add-event",
        Summary = "Add an event to a screen or entity: expose one it has, tunnel an object's event, or create a new one.",
        Usage = "add-event <project> --element <element> (--expose <event> | --variable <variable> [--when before|after] | --object <object> --object-event <event> | --custom --name <name> [--delegate-type <type>] [--args-type <type>]) [--name <name>]",
        Description =
@"Adds the event the way the editor's Add Event window does, with its three choices:

  Expose an existing event: --expose <event> takes one of the events the element can expose
    (InitializeEvent, ResolutionOrOrientationChanged, BeforeCollision on a screen, Click on an IWindow
    entity, AfterVisibleSet on an IVisible entity, After<Variable>Set for a variable with CreatesEvent).
    --variable <variable> [--when before|after] is the same for a variable's change event (default after).
    The event keeps that name.
  Tunnel an event from another object: --object <object> --object-event <event>, for example a collision
    relationship's CollisionOccurred. The name defaults to <object><event>, as in the window.
  Create a new event: --custom --name <name>, raised from your code. --delegate-type is one of
    System.EventHandler (default), FlatRedBall.Gui.WindowEvent or System.Action<T>; --args-type <type> fills
    in T (and picks System.Action<T> when --delegate-type is not given).

Glue writes the event into the element's .Generated.Event.cs and adds an empty handler, On<name>, to the
element's hand-written .Event.cs (creating either file if needed). Then saves and regenerates the element.

The run fails (exit 1, changes not saved) when the element, object, object event or variable is unknown, the
name is taken or not a valid C# name, or the object's event already has an event in the element. The message
lists close matches or the valid choices.

With --json, ""edit"" holds {element, name, kind, sourceObject, sourceObjectEvent, sourceVariable, when,
delegateType, generatedFile, handlerFile, handler, handlerAdded}: kind is exposed, variable, tunneled or
custom; generatedFile and handlerFile are relative to the .csproj folder; handlerAdded is true when the
handler method was added to handlerFile.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity to add the event to. Required."),
            new("name", "name", "Event name. Required with --custom. Defaults to <object><event> with --object; an\nexposed event keeps its own name."),
            new("expose", "event", "Expose this event of the element (the window's \"Expose an existing event\")."),
            new("variable", "variable", "Expose the change event of this variable (Visible, or one with CreatesEvent)."),
            new("when", "before|after", "With --variable: before or after the variable is set. Default: after."),
            new("object", "object", "Tunnel an event of this object (the window's \"Tunnel an event from another object\")."),
            new("object-event", "event", "With --object: the object's event, such as CollisionOccurred."),
            new("custom", null, "Create a new event (the window's \"Create a new event\"). Needs --name."),
            new("delegate-type", "type", "With --custom: System.EventHandler (default), FlatRedBall.Gui.WindowEvent or\nSystem.Action<T> (short names work too)."),
            new("args-type", "type", "With --custom: the type T for System.Action<T>, such as int or Entities.Enemy."),
        },
        Examples = new[]
        {
            ("add-event MyGame --element GameScreen --object PlayerVsEnemy --object-event CollisionOccurred", "Handle a collision relationship's CollisionOccurred in GameScreen (event PlayerVsEnemyCollisionOccurred)."),
            ("add-event MyGame --element GameScreen --object PuckVsGoal --object-event CollisionOccurred --name PuckVsGoalCollided --json", "Same with a chosen name, JSON output."),
            ("add-event MyGame --element Entities\\Player --variable Health --when after", "Run code after Player's Health is set (Health needs CreatesEvent)."),
            ("add-event MyGame --element GameScreen --expose InitializeEvent", "Expose GameScreen's InitializeEvent."),
            ("add-event MyGame --element Entities\\Player --custom --name Died --args-type int", "Add a new event Died of type System.Action<int> to Player."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record AddedEvent(string Element, string Name, string Kind, string? SourceObject, string? SourceObjectEvent,
        string? SourceVariable, string? When, string? DelegateType, string GeneratedFile, string HandlerFile, string Handler,
        bool HandlerAdded);

    static string? ValidateArguments(CliContext context)
    {
        if (!context.Has("element"))
        {
            return "add-event needs --element.";
        }
        var modes = new[] { "expose", "variable", "object", "custom" }.Count(context.Has);
        if (modes != 1)
        {
            return "add-event needs exactly one of --expose, --variable, --object or --custom.";
        }
        if (context.Has("object") != context.Has("object-event"))
        {
            return "--object and --object-event go together.";
        }
        if (context.Has("when") && !context.Has("variable"))
        {
            return "--when goes with --variable.";
        }
        if (context.Has("when") && ParseWhen(context.Get("when")!) == null)
        {
            return "--when must be before or after.";
        }
        if ((context.Has("delegate-type") || context.Has("args-type")) && !context.Has("custom"))
        {
            return "--delegate-type and --args-type go with --custom.";
        }
        if (context.Has("custom") && !context.Has("name"))
        {
            return "--custom needs --name.";
        }
        return null;
    }

    static BeforeOrAfter? ParseWhen(string text) => text.ToLowerInvariant() switch
    {
        "before" or "beforeset" => BeforeOrAfter.Before,
        "after" or "afterset" => BeforeOrAfter.After,
        _ => null,
    };

    static async Task RunAsync(CliContext context)
    {
        var element = CliLookup.FindElement(context.Get("element")!, out var elementError);
        if (element == null)
        {
            context.Fail(elementError!);
            return;
        }
        GlueState.Self.CurrentElement = element;

        var viewModel = new AddEventViewModel();
        string kind;
        if (context.Has("object"))
        {
            kind = "tunneled";
            if (!SetTunnel(context, element, viewModel)) { return; }
        }
        else if (context.Has("custom"))
        {
            kind = "custom";
            if (!SetCustom(context, viewModel)) { return; }
        }
        else
        {
            kind = context.Has("variable") ? "variable" : "exposed";
            if (!SetExposed(context, element, viewModel)) { return; }
        }

        var name = viewModel.EventName;
        // NameVerifier does not check events, so the editor would add a second event of the same name.
        if (element.GetEvent(name) != null)
        {
            context.Fail($"{element.Name} already has an event named {name}." +
                (kind == "tunneled" ? " Pass a different --name." : ""));
            return;
        }
        // IsEventNameValid returns true when the name is NOT valid.
        if (Builder.Get<NameVerifier>().IsEventNameValid(name, element, out var whyNameIsInvalid))
        {
            context.Fail($"'{name}' is not a valid event name in {element.Name}: {whyNameIsInvalid}");
            return;
        }

        var projectDirectory = GlueState.Self.CurrentGlueProjectDirectory;
        var generatedFile = EventManager.GetGeneratedEventFileNameForElement(element);
        var handlerFile = EventResponseSave.GetCustomEventFileNameForElement(element);
        var handlerPath = Path.Combine(projectDirectory, handlerFile);
        var handler = "On" + name;
        var hadHandler = HasMethod(handlerPath, handler);

        await GlueCommands.Self.GluxCommands.ElementCommands.AddEventToElement(viewModel, element);
        await TaskManager.Self.WaitForAllTasksFinished();

        var added = new AddedEvent(
            Element: element.Name,
            Name: name,
            Kind: kind,
            SourceObject: viewModel.TunnelingObject,
            SourceObjectEvent: viewModel.TunnelingEvent,
            SourceVariable: viewModel.SourceVariable,
            When: viewModel.SourceVariable != null ? viewModel.BeforeOrAfter.ToString().ToLowerInvariant() : null,
            DelegateType: string.IsNullOrEmpty(viewModel.DelegateType) ? null : viewModel.DelegateType,
            GeneratedFile: ToProjectRelative(generatedFile),
            HandlerFile: ToProjectRelative(handlerFile),
            Handler: handler,
            HandlerAdded: !hadHandler && HasMethod(handlerPath, handler));

        var what = kind switch
        {
            "tunneled" => $"{added.SourceObject}.{added.SourceObjectEvent}",
            "custom" => added.DelegateType!,
            "variable" => $"{added.When} {added.SourceVariable} is set",
            _ => "exposed",
        };
        context.Report(added, $"Added event {name} ({what}) to {element.Name}; handler {handler} in {added.HandlerFile}" +
            (added.HandlerAdded ? " (added)." : " (already there)."));
    }

    static bool SetExposed(CliContext context, GlueElement element, AddEventViewModel viewModel)
    {
        var exposable = element is EntitySave entity
            ? ExposedEventManager.GetExposableEventsFor(entity, false)
            : ExposedEventManager.GetExposableEventsFor((ScreenSave)element, false);

        ExposableEvent? match;
        if (context.Has("variable"))
        {
            var variableName = context.Get("variable")!;
            var when = context.Has("when") ? ParseWhen(context.Get("when")!)!.Value : BeforeOrAfter.After;
            var forVariable = exposable.Where(item => string.Equals(item.Variable, variableName, StringComparison.OrdinalIgnoreCase)).ToList();
            match = forVariable.FirstOrDefault(item => item.BeforeOrAfter == when);
            if (match == null)
            {
                var variablesWithEvents = exposable.Where(item => item.Variable != null).Select(item => item.Variable!).Distinct().ToList();
                var variable = element.GetCustomVariableRecursively(variableName);
                context.Fail(variable != null
                    ? $"{element.Name}.{variable.Name} does not raise events. Set its CreatesEvent to true first." +
                        (variablesWithEvents.Count > 0 ? " Variables that do: " + string.Join(", ", variablesWithEvents) + "." : "")
                    : $"{element.Name} has no variable named '{variableName}'." + CliLookup.DescribeCloseMatches(variableName,
                        element.CustomVariables.Select(item => item.Name)));
                return false;
            }
        }
        else
        {
            var eventName = context.Get("expose")!;
            match = exposable.FirstOrDefault(item => item.Name == eventName) ??
                exposable.FirstOrDefault(item => string.Equals(item.Name, eventName, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                context.Fail($"{element.Name} has no event '{eventName}' to expose." +
                    CliLookup.DescribeCloseMatches(eventName, exposable.Select(item => item.Name)) +
                    " To handle an object's event use --object, for a new event --custom.");
                return false;
            }
        }

        if (context.Has("name") && context.Get("name") != match.Name)
        {
            context.Fail($"An exposed event keeps its own name, {match.Name}; drop --name.");
            return false;
        }

        // What the window's ShowAddNewEventDialog copies from its Expose choice.
        viewModel.DesiredEventType = CustomEventType.Exposed;
        viewModel.EventName = match.Name;
        viewModel.SourceVariable = match.Variable;
        viewModel.BeforeOrAfter = match.BeforeOrAfter;
        viewModel.DelegateType = "";
        return true;
    }

    static bool SetTunnel(CliContext context, GlueElement element, AddEventViewModel viewModel)
    {
        var objectName = context.Get("object")!;
        var nos = element.GetNamedObjectRecursively(objectName) ??
            element.AllNamedObjects.FirstOrDefault(item => string.Equals(item.InstanceName, objectName, StringComparison.OrdinalIgnoreCase));
        if (nos == null)
        {
            context.Fail($"{element.Name} has no object named '{objectName}'." +
                CliLookup.DescribeCloseMatches(objectName, element.AllNamedObjects.Select(item => item.InstanceName)));
            return false;
        }

        var events = ExposedEventManager.GetExposableEventsFor(nos, null!);
        var eventName = context.Get("object-event")!;
        var match = events.FirstOrDefault(item => item.Name == eventName) ??
            events.FirstOrDefault(item => string.Equals(item.Name, eventName, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            context.Fail($"{nos.InstanceName} has no event '{eventName}'." + (events.Count == 0
                ? " Glue lists no events for it."
                : CliLookup.DescribeCloseMatches(eventName, events.Select(item => item.Name))));
            return false;
        }

        // The window leaves an object event that already has an event out of its list.
        var existing = element.Events.FirstOrDefault(item => item.SourceObject == nos.InstanceName && item.SourceObjectEvent == match.Name);
        if (existing != null)
        {
            context.Fail($"{nos.InstanceName}.{match.Name} already has the event {existing.EventName} in {element.Name}.");
            return false;
        }

        // What the window's ShowAddNewEventDialog copies from its Tunnel choice; the name box defaults to object + event.
        viewModel.DesiredEventType = CustomEventType.Tunneled;
        viewModel.TunnelingObject = nos.InstanceName;
        viewModel.TunnelingEvent = match.Name;
        viewModel.EventName = context.Get("name") ?? nos.InstanceName + match.Name;
        viewModel.DelegateType = "";
        return true;
    }

    static bool SetCustom(CliContext context, AddEventViewModel viewModel)
    {
        var available = AvailableDelegateTypeConverter.GetAvailableDelegates();
        var argsType = context.Get("args-type");
        string delegateType;
        if (context.Has("delegate-type"))
        {
            var requested = context.Get("delegate-type")!;
            var match = available.FirstOrDefault(item => string.Equals(item, requested, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Substring(item.LastIndexOf('.') + 1), requested, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                context.Fail($"'{requested}' is not a delegate type Glue offers. Available: {string.Join(", ", available)}.");
                return false;
            }
            delegateType = match;
        }
        else
        {
            delegateType = argsType != null ? available.First(item => item.Contains("<T>")) : available[0];
        }

        if (delegateType.Contains("<T>"))
        {
            if (string.IsNullOrWhiteSpace(argsType))
            {
                context.Fail($"{delegateType} needs --args-type for T.");
                return false;
            }
            // What the window's ResultDelegateType does with its generic type box.
            delegateType = delegateType.Replace("<T>", "<" + argsType + ">");
        }
        else if (argsType != null)
        {
            context.Fail($"{delegateType} takes no type argument; drop --args-type or use System.Action<T>.");
            return false;
        }

        viewModel.DesiredEventType = CustomEventType.New;
        viewModel.EventName = context.Get("name")!;
        viewModel.DelegateType = delegateType;
        return true;
    }

    static bool HasMethod(string file, string method) =>
        File.Exists(file) && System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(file), $@"\b{method}\s*\(");

    static string ToProjectRelative(string relativeToProject) => relativeToProject.Replace('\\', '/');
}
