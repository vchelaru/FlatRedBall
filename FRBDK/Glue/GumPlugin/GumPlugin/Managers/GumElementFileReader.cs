using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Serialization.Json;
using System.IO;

namespace GumPlugin.Managers;

/// <summary>
/// Reads a single Gum element or behavior file from disk, picking the XML or JSON deserializer from
/// the file's own extension. Mirrors Gum's rule that the extension alone decides the format; there is
/// no content sniffing between XML and JSON.
/// </summary>
public static class GumElementFileReader
{
    public static T ReadElement<T>(string absoluteFile, int projectVersion) where T : ElementSave, new()
    {
        if (IsJson(absoluteFile))
        {
            return GumJsonFileSerializer.DeserializeElement<T>(File.ReadAllText(absoluteFile));
        }

        var (content, _) = GumFileSerializer.ReadAndDetectFormat(absoluteFile);
        return GumFileSerializer.DeserializeElementSave<T>(content, projectVersion);
    }

    public static BehaviorSave ReadBehavior(string absoluteFile, int projectVersion)
    {
        if (IsJson(absoluteFile))
        {
            return GumJsonFileSerializer.DeserializeBehavior(File.ReadAllText(absoluteFile));
        }

        var (content, _) = GumFileSerializer.ReadAndDetectFormat(absoluteFile);
        return GumFileSerializer.DeserializeBehaviorSave(content, projectVersion);
    }

    static bool IsJson(string absoluteFile) => GumFileExtensions.IsJsonFile(absoluteFile);
}
