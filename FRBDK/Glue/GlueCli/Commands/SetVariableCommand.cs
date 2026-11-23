using System.Globalization;
using System.Reflection;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Instructions.Reflection;

namespace GlueCli.Commands;

/// <summary>
/// <c>set-variable</c>: sets a screen's or entity's variable default through
/// <see cref="FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.IElementCommands.HandleSetVariable"/>,
/// or a variable on one of its objects through
/// <see cref="FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.IGluxCommands.SetVariableOnAsync"/>,
/// the calls the property grid makes. The grid hands those calls a value already converted to the variable's
/// type, so this command converts the text itself; SetVariableOnAsync's own conversion does not know type
/// aliases such as "float" and would store a string.
/// </summary>
public static class SetVariableCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "set-variable",
        Summary = "Set a variable on a screen or entity, or on one of its objects (X on a Sprite, an exposed variable on an entity instance).",
        Usage = "set-variable <project> --element <element> [--object <name>] --variable <name> --value <text>",
        Description =
@"Sets the variable the way the editor's property grid does: same stored value, same plugin reactions and
code generation. Then saves the .gluj and element files and regenerates the changed elements' code.

Without --object, sets the default value of a variable the screen or entity defines (its Variables list).
With --object, sets a variable on that object in the element, at any depth (objects inside lists and
ShapeCollections included). An entity instance only has the variables its entity defines or exposes.

<text> is converted to the variable's type with invariant culture: 1.5, true, an enum value name. Files,
colors, states and CSV values are stored as text, as the editor stores them (--value Red for a Color, a
state's name for a state). --value ""<NONE>"" clears a Sprite or AnimationChainList reference.

Object properties (InstanceName, LayerOn, AttachToCamera, IsContainer, SourceClassType, ...) are not
variables; set-variable refuses them. set-property sets them.

Setting a screen's DefaultLayer asks whether to move the objects on the old layer to the new one. Pass
--move-objects to move them; without it the question goes unanswered and the run fails.

The run fails (exit 1, nothing saved) when the element, object or variable is unknown, or the text does not
convert to the variable's type. The message lists close matches or names the expected type.

With --json, ""edit"" holds {element, object, variable, type, oldValue, newValue, movedObjects}. object is
null for an element variable; oldValue is null when the variable was unset (the object used its default);
movedObjects lists the objects --move-objects put on the new layer.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that holds the variable or the object. Required."),
            new("object", "name", "Object in the element whose variable to set. Default: the element's own variable."),
            new("variable", "name", "Variable name. Required."),
            new("value", "text", "New value as text. Required; pass \"\" for an empty string."),
            new("move-objects", null, "A screen's DefaultLayer: move the objects on the old layer to the new one. Default:\nthe question is unanswered and the run fails."),
        },
        Examples = new[]
        {
            ("set-variable MyGame --element GameScreen --object SpriteInstance --variable X --value 120.5", "Move a Sprite in GameScreen."),
            ("set-variable MyGame --element Entities\\Player --variable MovementSpeed --value 300 --json", "Change the default of the Player entity's MovementSpeed variable, JSON output."),
            ("set-variable MyGame --element GameScreen --object Player1 --variable MovementSpeed --value 450", "Override an entity variable on one instance."),
            ("set-variable MyGame --element Entities\\Enemy --object CircleInstance --variable Color --value Red", "Set a Color (stored as the color's name)."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record SetVariable(string Element, string? Object, string Variable, string? Type, object? OldValue, object? NewValue,
        IReadOnlyList<string>? MovedObjects = null);

    /// <summary>MainScreenPlugin's question when a screen's DefaultLayer changes and objects use the old layer.</summary>
    const string MoveObjectsQuestion = "The following objects use the layer ";

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "variable", "value" })
        {
            if (!context.Has(required))
            {
                return $"set-variable needs --{required}.";
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

        if (context.Has("object"))
        {
            await SetOnObjectAsync(context, element);
        }
        else
        {
            await SetOnElementAsync(context, element);
        }
    }

    static async Task SetOnElementAsync(CliContext context, GlueElement element)
    {
        var variableName = context.Get("variable")!;
        var variable = element.CustomVariables.FirstOrDefault(item => item.Name == variableName) ??
            element.CustomVariables.FirstOrDefault(item => string.Equals(item.Name, variableName, StringComparison.OrdinalIgnoreCase));
        var isNew = false;
        if (variable == null)
        {
            // Variables every screen has (DefaultLayer) or a plugin adds. The property grid creates the element's
            // CustomVariable for these when it first shows the element; do the same, once the value converts.
            var definitions = PluginManager.GetVariableDefinitionsFor(element).ToList();
            if (element is ScreenSave)
            {
                definitions.InsertRange(0, AvailableAssetTypes.CommonAtis.Screen.VariableDefinitions);
            }
            var definition = definitions.FirstOrDefault(item => item.Name == variableName) ??
                definitions.FirstOrDefault(item => string.Equals(item.Name, variableName, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                if (IsPropertyName(element.GetType(), variableName))
                {
                    context.Fail($"'{variableName}' is a property of {element.Name}, not a variable. set-variable only sets variables.");
                    return;
                }
                var names = element.CustomVariables.Select(item => item.Name).Concat(definitions.Select(item => item.Name)).ToList();
                context.Fail($"{element.Name} has no variable '{variableName}'." +
                    (names.Count == 0 ? " It defines no variables." : CliLookup.DescribeCloseMatches(variableName, names)));
                return;
            }
            variable = new CustomVariable
            {
                Name = definition.Name,
                Type = definition.Type,
                DefaultValue = definition.DefaultValue,
                Category = definition.Category,
            };
            if (definition.CustomGetForcedOptionFunc != null)
            {
                variable.CustomGetForcedOptionsFunc = forElement => definition.CustomGetForcedOptionFunc(forElement, null, null);
            }
            isNew = true;
        }

        var keepsText = variable.GetIsVariableState(element) || variable.GetIsFile() || variable.GetIsObjectType() || IsColor(variable.Type);
        var typeName = string.IsNullOrEmpty(variable.OverridingPropertyType) ? variable.Type : variable.OverridingPropertyType;
        if (!TryConvert(context, context.Get("value")!, keepsText ? null : typeName, $"{element.Name}.{variable.Name}", out var value))
        {
            return;
        }

        if (isNew)
        {
            element.CustomVariables.Add(variable);
        }
        var oldValue = variable.DefaultValue;
        var moveObjects = context.Has("move-objects");
        var allObjects = GlueState.Self.CurrentGlueProject.Screens.Cast<GlueElement>().Concat(GlueState.Self.CurrentGlueProject.Entities)
            .SelectMany(item => item.AllNamedObjects.Select(nos => (element: item, nos))).ToList();
        var layersBefore = allObjects.Select(item => item.nos.LayerOn).ToList();
        // Without --move-objects the question is left to the runner, which reports it and fails the run.
        using (new CliDialogs(answerConfirm: (text, _, _) =>
            moveObjects && text.StartsWith(MoveObjectsQuestion, StringComparison.Ordinal) ? FlatRedBall.Glue.Controls.DialogButton.Yes : null))
        {
            await GlueCommands.Self.GluxCommands.ElementCommands.HandleSetVariable(variable, value, performSaveAndGenerateCode: true, updateUi: false);
            await TaskManager.Self.WaitForAllTasksFinished();
        }
        var movedObjects = allObjects.Where((item, index) => item.nos.LayerOn != layersBefore[index])
            .Select(item => $"{item.element.Name}.{item.nos.InstanceName}").ToList();

        Report(context, new SetVariable(element.Name, null, variable.Name, typeName, oldValue, variable.DefaultValue,
            movedObjects.Count > 0 ? movedObjects : null));
    }

    static async Task SetOnObjectAsync(CliContext context, GlueElement element)
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

        var variableName = context.Get("variable")!;
        var text = context.Get("value")!;
        var entity = ObjectFinder.Self.GetEntitySave(nos);

        string name;
        string? typeName;
        bool keepsText;
        VariableDefinition? definition = null;
        if (entity != null)
        {
            // SetVariableOnAsync only prints an error for a variable the entity does not have; refuse it here.
            var entityVariables = new[] { (GlueElement)entity }.Concat(ObjectFinder.Self.GetAllBaseElementsRecursively(entity))
                .SelectMany(item => item.CustomVariables).Select(item => item.Name).Distinct().ToList();
            var match = entityVariables.FirstOrDefault(item => item == variableName) ??
                entityVariables.FirstOrDefault(item => string.Equals(item, variableName, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                FailUnknownObjectVariable(context, element, nos, variableName,
                    $"{nos.InstanceName} is an instance of {entity.Name}, which has no variable '{variableName}'. " +
                    $"An entity instance can only set variables its entity defines or exposes; add or expose it on {entity.Name} first.",
                    entityVariables);
                return;
            }
            var variable = entity.GetCustomVariableRecursively(match);
            name = match;
            typeName = string.IsNullOrEmpty(variable.OverridingPropertyType) ? variable.Type : variable.OverridingPropertyType;
            keepsText = variable.GetIsVariableState(entity) || variable.GetIsFile() || variable.GetIsObjectType() || IsColor(variable.Type) ||
                NamedObjectPropertyGridDisplayer.GetIfIsCsv(nos, match);
        }
        else
        {
            var definitions = nos.GetAssetTypeInfo()?.VariableDefinitions ?? new List<VariableDefinition>();
            definition = definitions.FirstOrDefault(item => item.Name == variableName) ??
                definitions.FirstOrDefault(item => string.Equals(item.Name, variableName, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                FailUnknownObjectVariable(context, element, nos, variableName,
                    $"{nos.InstanceName} ({nos.SourceClassType ?? nos.SourceType.ToString()}) has no variable '{variableName}'.",
                    definitions.Select(item => item.Name).ToList());
                return;
            }
            name = definition.Name;
            typeName = definition.Type;
            // The same types SetVariableOnInner leaves as text.
            keepsText = typeName == null || IsColor(typeName) || CustomVariableTypeExtensions.GetIsFile(typeName) ||
                CustomVariableTypeExtensions.GetIsObjectType(typeName) || NamedObjectPropertyGridDisplayer.GetIfIsCsv(nos, name);
        }

        var oldValue = nos.GetCustomVariable(name)?.Value;
        var gluxCommands = GlueCommands.Self.GluxCommands;

        if (definition?.CustomVariableSet != null)
        {
            // A variable the editor computes (a Sprite's ColorHex sets Red, Green and Blue). The grid hands it the
            // text as typed.
            definition.CustomVariableSet(element, nos, name, text);
            Report(context, new SetVariable(element.Name, nos.InstanceName, name, typeName, oldValue, text));
            return;
        }

        object? value;
        if (text == "<NONE>" && (typeName == "AnimationChainList" || typeName == "Sprite"))
        {
            // NamedObjectSaveVariableDataGridItem.HandleVariableSet: "<NONE>" clears the reference, and clearing the
            // AnimationChainList also clears CurrentChainName.
            value = null;
            if (typeName == "AnimationChainList")
            {
                await gluxCommands.SetVariableOnAsync(nos, "CurrentChainName", null, performSaveAndGenerateCode: false, updateUi: false, recordUndo: false);
            }
        }
        else if (!TryConvert(context, text, keepsText ? null : typeName, $"{nos.InstanceName}.{name}", out value))
        {
            return;
        }

        await gluxCommands.SetVariableOnAsync(nos, name, value, performSaveAndGenerateCode: true, updateUi: false, recordUndo: false);

        Report(context, new SetVariable(element.Name, nos.InstanceName, name, typeName, oldValue, nos.GetCustomVariable(name)?.Value));
    }

    static void FailUnknownObjectVariable(CliContext context, GlueElement element, NamedObjectSave nos, string variableName,
        string message, IReadOnlyCollection<string> variables)
    {
        if (IsPropertyName(typeof(NamedObjectSave), variableName) ||
            nos.Properties.Any(item => string.Equals(item.Name, variableName, StringComparison.OrdinalIgnoreCase)))
        {
            context.Fail($"'{variableName}' is a property of the object {nos.InstanceName}, not a variable. set-variable only sets variables.");
            return;
        }
        context.Fail(message + (variables.Count == 0 ? " It has no variables." : CliLookup.DescribeCloseMatches(variableName, variables)));
    }

    /// <summary>
    /// <paramref name="text"/> as <paramref name="typeName"/>, or the text itself when the type is null, string,
    /// or one Glue cannot resolve (as the editor stores it). Fails with the expected type when it does not parse.
    /// </summary>
    static bool TryConvert(CliContext context, string text, string? typeName, string target, out object? value)
    {
        value = text;
        if (typeName == null || typeName == "string")
        {
            return true;
        }
        var type = TypeManager.GetTypeFromString(typeName);
        if (type == null || type == typeof(string))
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            context.Fail($"{target} is a {typeName}; an empty --value is not one.");
            return false;
        }

        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            // Most branches of the conversion pass InvariantCulture; this covers the ones that do not.
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            value = PropertyValuePair.ConvertStringToType(text, type);
            return true;
        }
        catch (Exception e) when (e is FormatException or OverflowException or ArgumentException or InvalidOperationException)
        {
            var valid = type.IsEnum ? " Valid values: " + string.Join(", ", Enum.GetNames(type)) + "." : "";
            context.Fail($"'{text}' is not a valid {typeName} for {target}.{valid}");
            return false;
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    static void Report(CliContext context, SetVariable result)
    {
        var target = result.Object != null ? $"{result.Object}.{result.Variable} in {result.Element}" : $"{result.Element}.{result.Variable}";
        var text = $"Set {target} ({result.Type}): {Describe(result.OldValue)} -> {Describe(result.NewValue)}.";
        if (result.MovedObjects != null)
        {
            text += Environment.NewLine + "  moved to the new layer: " + string.Join(", ", result.MovedObjects);
        }
        context.Report(result, text);
    }

    static string Describe(object? value) => value switch
    {
        null => "(default)",
        string text => $"\"{text}\"",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    static bool IsColor(string? typeName) => typeName is "Color" or "Microsoft.Xna.Framework.Color";

    static bool IsPropertyName(Type type, string name) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));
}
