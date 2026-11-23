using System.Globalization;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;

namespace GlueCli.Commands;

/// <summary>
/// <c>add-variable</c>: adds a variable to a screen or entity the way the Add Variable window does. The command
/// fills the window's own view model (<see cref="DialogCommands.CreateAddVariableViewModel"/>, same defaults),
/// checks it with the window's OK check (<see cref="DialogCommands.GetAddVariableFailure"/>), and adds it through
/// <see cref="DialogCommands.AddVariableFromViewModelAsync"/>, which ends in
/// <c>ElementCommands.AddCustomVariableToElementAsync</c>. A default value is then set the way the property grid
/// sets one (<c>ElementCommands.HandleSetVariable</c>).
/// </summary>
public static class AddVariableCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "add-variable",
        Summary = "Add a variable to a screen or entity: a new one, one tunneling into an object's variable, or an exposed built-in one.",
        Usage = "add-variable <project> --element <element> (--name <name> --type <type> | --tunnel <object>.<variable> [--name <name>] | --expose <variable>) [options]",
        Description =
@"Adds the variable the way the editor's Add Variable window does, with the window's defaults and checks,
then saves the element and regenerates its code. The window's three kinds:

  --name N --type T        A new variable. T is a type the window lists: float, int, bool, string, double,
                           decimal, byte, long, the nullable ones (float?), Color, Texture2D, Keys, a CSV's
                           class, or a state category (Entities\Enemy.Movement or the category's name).
  --tunnel Obj.Var         A variable that sets Var on the object Obj (an object at any depth). Its name
                           defaults to ObjVar as in the window; --name overrides it. Its default is what the
                           object has Var set to.
  --expose Var             A built-in variable of the element exposed in Glue (X, Visible, Enabled...).

The window's other options, as flags (defaults are the window's):
  --set-by-derived true|false   Derived elements can set it (checked by default). Ignored when static.
  --static true|false           A static (shared) variable. Default: checked when every other variable
                                except X, Y and Z is static, unless tunneling.
  --list                        A List<T> of a new string variable (float and int too on JSON projects).
  --category C                  Category it is listed under. The window replaces it with the object's
                                category for a tunneled variable.
  --summary S                   Summary (the XML doc comment in generated code).
  --overriding-type T           Tunneled only: expose the variable as another type, converted with
  --type-converter C            the given converter (the window's first converter by default).
--default sets the default value after adding it, converted to the type as set-variable does.

The run fails (exit 1, nothing saved) when the element, type, object or variable is unknown, the name is
taken or invalid, the object's variable is already tunneled, or the value does not convert. The message
says why and lists close matches.

With --json, ""edit"" holds {element, name, kind, type, sourceObject, sourceObjectProperty, defaultValue,
setByDerived, isShared, category, changedElements}: kind is new, tunneled or exposed, changedElements every
screen and entity the add changed (derived elements get the variable too).",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity to add the variable to. Required."),
            new("name", "name", "Variable name. Required with --type; optional with --tunnel."),
            new("type", "type", "Type of a new variable."),
            new("tunnel", "object.variable", "Tunnel into this object's variable."),
            new("expose", "variable", "Expose this built-in variable of the element."),
            new("default", "text", "Default value, converted to the variable's type."),
            new("set-by-derived", "true|false", "Whether derived elements can set it. Default: true."),
            new("static", "true|false", "Whether it is static. Default: the window's (see above)."),
            new("list", null, "Make a new variable a List of its type."),
            new("category", "name", "Category to list it under."),
            new("summary", "text", "Summary text."),
            new("overriding-type", "type", "Tunneled only: the type to expose it as."),
            new("type-converter", "name", "Tunneled only, with --overriding-type: the converter."),
        },
        Examples = new[]
        {
            ("add-variable MyGame --element Entities\\Player --name JumpSpeed --type float --default 250", "A new float variable with a default."),
            ("add-variable MyGame --element Entities\\Player --tunnel SpriteInstance.Visible --name SpriteVisible --json", "Tunnel into an object's variable, JSON output."),
            ("add-variable MyGame --element Entities\\Player --expose Visible", "Expose the entity's own Visible."),
            ("add-variable MyGame --element GameScreen --name EnemyNames --type string --list --static true", "A static List<string>."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record AddedVariable(string Element, string Name, string Kind, string Type, string? SourceObject,
        string? SourceObjectProperty, object? DefaultValue, bool SetByDerived, bool IsShared, string? Category,
        IReadOnlyList<string> ChangedElements);

    static string? ValidateArguments(CliContext context)
    {
        if (!context.Has("element"))
        {
            return "add-variable needs --element.";
        }
        var kinds = new[] { "type", "tunnel", "expose" }.Count(context.Has);
        if (kinds != 1)
        {
            return "Pass exactly one of --type (with --name), --tunnel or --expose.";
        }
        if (context.Has("type") && !context.Has("name"))
        {
            return "--type needs --name.";
        }
        if (context.Has("expose") && context.Has("name"))
        {
            return "--expose takes the variable's own name; --name does not apply.";
        }
        if (context.Has("list") && !context.Has("type"))
        {
            return "--list only applies to a new variable (--type).";
        }
        if ((context.Has("overriding-type") || context.Has("type-converter")) && !context.Has("tunnel"))
        {
            return "--overriding-type and --type-converter only apply to --tunnel.";
        }
        if (context.Has("type-converter") && !context.Has("overriding-type"))
        {
            return "--type-converter needs --overriding-type.";
        }
        var tunnel = context.Get("tunnel");
        if (context.Has("tunnel") && (tunnel == null || tunnel.IndexOf('.') <= 0 || tunnel.EndsWith('.')))
        {
            return "--tunnel takes <object>.<variable>, for example SpriteInstance.Visible.";
        }
        foreach (var flag in new[] { "set-by-derived", "static" })
        {
            if (context.Has(flag) && !bool.TryParse(context.Get(flag), out _))
            {
                return $"--{flag} takes true or false.";
            }
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
        GlueState.Self.CurrentElement = element;

        string kind;
        AddCustomVariableViewModel viewModel;
        if (context.Has("type"))
        {
            kind = "new";
            viewModel = DialogCommands.CreateAddVariableViewModel(element, CustomVariableType.New, "", "");
            if (!TrySetNewVariable(context, viewModel))
            {
                return;
            }
        }
        else if (context.Has("tunnel"))
        {
            kind = "tunneled";
            var tunnel = context.Get("tunnel")!;
            var dot = tunnel.IndexOf('.');
            viewModel = DialogCommands.CreateAddVariableViewModel(element, CustomVariableType.Tunneled, "", "");
            if (!TrySetTunnel(context, element, viewModel, tunnel.Substring(0, dot), tunnel.Substring(dot + 1)))
            {
                return;
            }
        }
        else
        {
            kind = "exposed";
            viewModel = DialogCommands.CreateAddVariableViewModel(element, CustomVariableType.Exposed, "", "");
            var exposed = Match(context.Get("expose")!, viewModel.AvailableExposedVariables);
            if (exposed == null)
            {
                context.Fail($"{element.Name} has no variable '{context.Get("expose")}' to expose." +
                    (viewModel.AvailableExposedVariables.Count == 0 ? " It has none left to expose."
                        : CliLookup.DescribeCloseMatches(context.Get("expose")!, viewModel.AvailableExposedVariables)));
                return;
            }
            viewModel.SelectedExposedVariable = exposed;
        }

        if (context.Has("category"))
        {
            viewModel.NewVariableCategory = context.Get("category");
        }
        if (context.Has("summary"))
        {
            viewModel.NewVariableSummary = context.Get("summary");
        }
        if (context.Has("set-by-derived"))
        {
            viewModel.SetByDerived = bool.Parse(context.Get("set-by-derived")!);
        }
        if (context.Has("static"))
        {
            viewModel.IsStatic = bool.Parse(context.Get("static")!);
        }

        var name = viewModel.ResultName;
        var failure = DialogCommands.GetAddVariableFailure(viewModel, element);
        if (failure != null)
        {
            failure = failure.Trim();
            context.Fail($"Cannot add {name} to {element.Name}: {failure}" + (failure.EndsWith('.') ? "" : ".") +
                (element.CustomVariables.Any(item => item.Name == name) ? " Pick another --name." : ""));
            return;
        }
        var inBase = element.GetCustomVariableRecursively(name);
        if (inBase != null && !inBase.SetByDerived)
        {
            // AddVariableFromViewModelAsync shows this as a message and adds nothing.
            context.Fail($"A base of {element.Name} already defines {name}, and it is not SetByDerived. " +
                "Set SetByDerived on the base's variable (set-variable-property) or pick another --name.");
            return;
        }

        object? defaultValue = null;
        if (context.Has("default"))
        {
            if (viewModel.IsList)
            {
                context.Fail("--default does not take a list value; set list items in the editor or the .glej.");
                return;
            }
            var probe = new CustomVariable { Name = name, Type = viewModel.ResultType };
            if (kind == "tunneled" && viewModel.SelectedOverridingType != "<none>")
            {
                probe.OverridingPropertyType = viewModel.SelectedOverridingType;
            }
            var keepsText = probe.GetIsVariableState(element) || probe.GetIsFile() || probe.GetIsObjectType() ||
                SetVariableCommand.IsColor(probe.Type);
            var typeName = string.IsNullOrEmpty(probe.OverridingPropertyType) ? probe.Type : probe.OverridingPropertyType;
            if (!SetVariableCommand.TryConvert(context, context.Get("default")!, keepsText ? null : typeName, $"{element.Name}.{name}", out defaultValue))
            {
                return;
            }
        }

        var before = CliChanges.Snapshot();
        CustomVariable? added = null;
        // The window's OK queues the add as one task.
        await TaskManager.Self.AddAsync(async () => { added = await DialogCommands.AddVariableFromViewModelAsync(viewModel, element); },
            $"Adding variable {name} through GlueCli");
        await TaskManager.Self.WaitForAllTasksFinished();
        if (added == null)
        {
            context.Fail($"Glue did not add {name} to {element.Name}.");
            return;
        }

        if (context.Has("default"))
        {
            await GlueCommands.Self.GluxCommands.ElementCommands.HandleSetVariable(added, defaultValue, performSaveAndGenerateCode: true, updateUi: false);
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        var type = string.IsNullOrEmpty(added.OverridingPropertyType) ? added.Type : added.OverridingPropertyType;
        var result = new AddedVariable(element.Name, added.Name, kind, type, added.SourceObject, added.SourceObjectProperty,
            added.DefaultValue, added.SetByDerived, added.IsShared, added.Category, CliChanges.ChangedElements(before));
        var source = added.SourceObject != null ? $", tunnels {added.SourceObject}.{added.SourceObjectProperty}" : "";
        var lines = new List<string>
        {
            $"Added {kind} variable {element.Name}.{added.Name} ({type}{source}), default {DescribeValue(added.DefaultValue)}.",
            $"  SetByDerived: {added.SetByDerived}, static: {added.IsShared}" + (string.IsNullOrEmpty(added.Category) ? "" : $", category: {added.Category}"),
        };
        if (result.ChangedElements.Count > 0)
        {
            lines.Add("  changed elements: " + string.Join(", ", result.ChangedElements));
        }
        context.Report(result, string.Join(Environment.NewLine, lines));
    }

    static bool TrySetNewVariable(CliContext context, AddCustomVariableViewModel viewModel)
    {
        // The window lists state categories behind a checkbox; any listed type is accepted here.
        viewModel.IsShowStateCategoriesChecked = true;
        var types = viewModel.AvailableNewVariableTypes;
        var requested = context.Get("type")!;
        var type = Match(requested, types);
        if (type == null)
        {
            // Color for Microsoft.Xna.Framework.Color, Movement for Entities.Enemy.Movement, when unique.
            var byShortName = types.Where(item => string.Equals(item.Substring(item.LastIndexOf('.') + 1), requested, StringComparison.OrdinalIgnoreCase)).ToList();
            type = byShortName.Count == 1 ? byShortName[0] : null;
        }
        if (type == null)
        {
            context.Fail($"'{requested}' is not a type the Add Variable window offers." +
                CliLookup.DescribeCloseMatches(requested, types.Select(item => item.Replace('.', '\\'))).Replace('\\', '.'));
            return false;
        }
        viewModel.NewVariableName = context.Get("name");
        viewModel.SelectedNewType = type;
        if (context.Has("list"))
        {
            if (!viewModel.CanBeList)
            {
                context.Fail($"A {type} variable cannot be a list. The window allows lists of string, and of float and int on JSON projects.");
                return false;
            }
            viewModel.IsList = true;
        }
        return true;
    }

    static bool TrySetTunnel(CliContext context, GlueElement element, AddCustomVariableViewModel viewModel, string objectName, string variableName)
    {
        var objects = viewModel.AvailableTunneledObjects;
        var sourceObject = Match(objectName, objects);
        if (sourceObject == null)
        {
            context.Fail($"{element.Name} has no object '{objectName}' to tunnel into." +
                (objects.Count == 0 ? " It has no objects." : CliLookup.DescribeCloseMatches(objectName, objects)));
            return false;
        }
        viewModel.SelectedTunneledObject = sourceObject;

        var variables = viewModel.AvailableTunneledVariableNames ?? new List<string>();
        var variable = Match(variableName, variables);
        if (variable == null)
        {
            var taken = element.CustomVariables.FirstOrDefault(item => item.SourceObject == sourceObject &&
                string.Equals(item.SourceObjectProperty, variableName, StringComparison.OrdinalIgnoreCase));
            context.Fail(taken != null
                ? $"{element.Name}.{taken.Name} already tunnels into {sourceObject}.{taken.SourceObjectProperty}."
                : $"{sourceObject} has no variable '{variableName}' to tunnel into." +
                    (variables.Count == 0 ? " It has none." : CliLookup.DescribeCloseMatches(variableName, variables)));
            return false;
        }
        // Also fills the window's name, ObjVar.
        viewModel.SelectedTunneledVariableName = variable;
        if (context.Has("name"))
        {
            viewModel.AlternativeName = context.Get("name");
        }

        if (context.Has("overriding-type"))
        {
            var overriding = Match(context.Get("overriding-type")!, viewModel.AvailableOverridingTypes);
            if (overriding == null)
            {
                context.Fail($"'{context.Get("overriding-type")}' is not an overriding type the window offers. Valid values: " +
                    string.Join(", ", viewModel.AvailableOverridingTypes) + ".");
                return false;
            }
            viewModel.SelectedOverridingType = overriding;
        }
        if (context.Has("type-converter"))
        {
            var converter = Match(context.Get("type-converter")!, viewModel.AvailableTypeConverters);
            if (converter == null)
            {
                context.Fail($"'{context.Get("type-converter")}' is not a type converter. Valid values: " +
                    string.Join(", ", viewModel.AvailableTypeConverters) + ".");
                return false;
            }
            viewModel.SelectedTypeConverter = converter;
        }
        return true;
    }

    /// <summary>The item equal to <paramref name="text"/>, exactly or else ignoring case.</summary>
    static string? Match(string text, IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.FirstOrDefault(item => item == text) ??
            list.FirstOrDefault(item => string.Equals(item, text, StringComparison.OrdinalIgnoreCase));
    }

    static string DescribeValue(object? value) => value switch
    {
        null => "(none)",
        string text => $"\"{text}\"",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
