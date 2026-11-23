using System.Globalization;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCli;

/// <summary>
/// What the collision commands share: the Collision Plugin's <see cref="ICollisionRelationshipCommands"/>, and turning
/// option text into setting values and setting values into reports.
/// </summary>
internal static class CliCollision
{
    /// <summary>The CollisionType values by the short names the commands accept.</summary>
    static readonly Dictionary<string, string> CollisionTypeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["none"] = "NoPhysics",
        ["move"] = "MoveCollision",
        ["move-soft"] = "MoveSoftCollision",
        ["bounce"] = "BounceCollision",
        ["platformer"] = "PlatformerSolidCollision",
        ["platformer-solid"] = "PlatformerSolidCollision",
        ["platformer-cloud"] = "PlatformerCloudCollision",
        ["delegate"] = "DelegateCollision",
        ["stacking"] = "StackingCollision",
    };

    public const string CollisionTypeHelp = "none, move, move-soft, bounce, platformer (solid), platformer-cloud, delegate, stacking";

    /// <summary>The Collision Plugin's commands, or null after failing the run when the plugin is not loaded.</summary>
    public static ICollisionRelationshipCommands? GetCommands(CliContext context)
    {
        var commands = GlueCommands.Self.CollisionRelationshipCommands;
        if (commands == null)
        {
            context.Fail("The Collision Plugin is not loaded, so GlueCli cannot edit collision relationships.");
        }
        return commands;
    }

    public static CollisionRelationshipSetting? FindSetting(ICollisionRelationshipCommands commands, string name) =>
        commands.Settings.FirstOrDefault(item => item.Name == name) ??
        commands.Settings.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Every setting's value, enums by name, so --json shows the names set-collision-property takes.</summary>
    public static SortedDictionary<string, object?> Values(ICollisionRelationshipCommands commands, NamedObjectSave relationship) =>
        new(commands.Settings.ToDictionary(setting => setting.Name, setting => ForReport(commands.GetValue(relationship, setting.Name))),
            StringComparer.Ordinal);

    public static object? ForReport(object? value) => value is Enum ? value.ToString() : value;

    /// <summary>
    /// <paramref name="text"/> as a value of <paramref name="setting"/>. <paramref name="options"/>, when given, are the
    /// only allowed text values (a dropdown), matched case-insensitively and returned as listed.
    /// </summary>
    public static bool TryConvert(CollisionRelationshipSetting setting, string text, IReadOnlyList<string>? options, out object? value, out string? error)
    {
        value = null;
        error = null;
        var type = setting.Type;
        if (type == typeof(string))
        {
            if (options == null)
            {
                value = text;
                return true;
            }
            value = options.FirstOrDefault(item => item == text) ??
                options.FirstOrDefault(item => string.Equals(item, text, StringComparison.OrdinalIgnoreCase));
            if (value == null)
            {
                error = $"'{text}' is not a valid {setting.Name}." +
                    (options.Count == 0 ? " Nothing can be picked for it here." : " Valid values: " + string.Join(", ", options) + ".");
                return false;
            }
            return true;
        }
        if (type.IsEnum)
        {
            var isCollisionType = setting.Name == "CollisionType";
            var name = isCollisionType && CollisionTypeAliases.TryGetValue(text, out var alias) ? alias : text;
            var enumName = Enum.GetNames(type).FirstOrDefault(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
            if (enumName == null)
            {
                error = $"'{text}' is not a valid {setting.Name}. Valid values: " +
                    (isCollisionType ? CollisionTypeHelp : string.Join(", ", Enum.GetNames(type))) + ".";
                return false;
            }
            value = Enum.Parse(type, enumName);
            return true;
        }
        if (type == typeof(bool))
        {
            if (bool.TryParse(text, out var flag))
            {
                value = flag;
                return true;
            }
            error = $"'{text}' is not a valid {setting.Name}. Valid values: true, false.";
            return false;
        }
        if (type == typeof(float) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            value = number;
            return true;
        }
        error = $"'{text}' is not a valid {setting.Name} ({type.Name}).";
        return false;
    }

    public static string Describe(object? value) => value switch
    {
        null => "(none)",
        bool flag => flag ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
