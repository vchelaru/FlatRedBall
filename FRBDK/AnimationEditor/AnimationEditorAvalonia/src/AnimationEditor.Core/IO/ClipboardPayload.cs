using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.IO;

namespace AnimationEditor.Core.IO;

/// <summary>
/// Handles the textual clipboard format used for copy/paste (IO14).
///
/// Format:  "TypeName:&lt;xml&gt;"
///   • TypeName is the simple C# type name, e.g.
///     "List&lt;AnimationChainSave&gt;", "List&lt;AnimationFrameSave&gt;",
///     "AxisAlignedRectangleSave", or "CircleSave".
///   • The XML is produced by <see cref="FlatRedBall.IO.FileManager.XmlSerialize"/>.
///
/// This class handles only serialization/deserialization — clipboard I/O is the
/// responsibility of the app layer.
/// </summary>
public static class ClipboardPayload
{
    // ── Serialization ─────────────────────────────────────────────────────

    public static string Serialize(List<AnimationChainSave> chains)
        => Encode(chains);

    public static string Serialize(List<AnimationFrameSave> frames)
        => Encode(frames);

    public static string Serialize(AxisAlignedRectangleSave rectangle)
        => Encode(rectangle);

    public static string Serialize(CircleSave circle)
        => Encode(circle);

    // ── Deserialization ───────────────────────────────────────────────────

    /// <summary>
    /// Attempts to parse a clipboard string into one of the supported payload types.
    /// Returns <c>true</c> when the string was valid and the appropriate out-parameter
    /// is populated; all other out-parameters will be <c>null</c>.
    /// Returns <c>false</c> when the string is unrecognised or malformed.
    /// </summary>
    public static bool TryDeserialize(
        string? text,
        out List<AnimationChainSave>? chains,
        out List<AnimationFrameSave>? frames,
        out AxisAlignedRectangleSave? rectangle,
        out CircleSave? circle)
    {
        chains    = null;
        frames    = null;
        rectangle = null;
        circle    = null;

        if (string.IsNullOrEmpty(text)) return false;
        int sep = text.IndexOf(':');
        if (sep < 0) return false;

        var typeName = text[..sep];
        var xml      = text[(sep + 1)..];

        try
        {
            if (typeName == TypeName<List<AnimationChainSave>>())
            {
                chains = FileManager.XmlDeserializeFromString<List<AnimationChainSave>>(xml);
                return chains != null;
            }
            if (typeName == TypeName<List<AnimationFrameSave>>())
            {
                frames = FileManager.XmlDeserializeFromString<List<AnimationFrameSave>>(xml);
                return frames != null;
            }
            if (typeName == nameof(AxisAlignedRectangleSave))
            {
                rectangle = FileManager.XmlDeserializeFromString<AxisAlignedRectangleSave>(xml);
                return rectangle != null;
            }
            if (typeName == nameof(CircleSave))
            {
                circle = FileManager.XmlDeserializeFromString<CircleSave>(xml);
                return circle != null;
            }
        }
        catch
        {
            // Malformed XML — return false
        }

        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string Encode<T>(T obj)
    {
        FileManager.XmlSerialize(obj, out string xml);
        return $"{TypeName<T>()}:{xml}";
    }

    private static string TypeName<T>()
    {
        var t = typeof(T);
        if (t.IsGenericType)
            return $"List<{t.GenericTypeArguments[0].Name}>";
        return t.Name;
    }
}
