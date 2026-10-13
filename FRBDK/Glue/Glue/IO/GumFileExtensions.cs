using Gum.DataTypes;
using System;

namespace FlatRedBall.Glue.IO;

/// <summary>
/// Maps between Gum's XML and JSON file extensions. Every JSON extension is its XML counterpart with
/// the trailing "x" swapped for "j" (gumx->gumj, gusx->gusj, gucx->gucj, gutx->gutj, behx->behj,
/// ganx->ganj), so Glue code can compare against the XML extension and accept both formats.
/// </summary>
public static class GumFileExtensions
{
    static readonly string[] JsonExtensions = { "gumj", "gusj", "gucj", "gutj", "behj", "ganj" };
    static readonly string[] XmlExtensions = { "gumx", "gusx", "gucx", "gutx", "behx", "ganx" };

    /// <summary>
    /// Returns the XML counterpart of a Gum JSON extension (no leading dot), lowercased.
    /// Any other extension is returned unchanged.
    /// </summary>
    public static string Normalize(string extension)
    {
        if (extension == null)
        {
            return null;
        }

        foreach (var json in JsonExtensions)
        {
            if (string.Equals(extension, json, StringComparison.OrdinalIgnoreCase))
            {
                return json.Substring(0, json.Length - 1) + "x";
            }
        }

        return extension;
    }

    /// <summary>
    /// True if the file name has the given XML extension (e.g. "gusx") or its JSON counterpart ("gusj").
    /// </summary>
    public static bool HasExtension(string fileName, string xmlExtension)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        var extension = Normalize(FlatRedBall.IO.FileManager.GetExtension(fileName));
        return string.Equals(extension, xmlExtension, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True if the file name has any Gum JSON extension (.gumj, .gusj, .gucj, .gutj, .behj, .ganj).</summary>
    public static bool IsJsonFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        var extension = FlatRedBall.IO.FileManager.GetExtension(fileName);
        return !string.Equals(Normalize(extension), extension, StringComparison.Ordinal);
    }

    /// <summary>
    /// True if the file is a Gum project, element, behavior or animation file in the format the loaded
    /// project does not use, e.g. a leftover .gusx beside a .gumj project. The project never loads it, so
    /// reacting to it would replace a loaded element with stale content.
    /// </summary>
    public static bool IsGumFileOfOtherFormat(string fileName, bool isJsonProject)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        var extension = Normalize(FlatRedBall.IO.FileManager.GetExtension(fileName));
        var isGumFile = Array.IndexOf(XmlExtensions, extension?.ToLowerInvariant()) >= 0;

        return isGumFile && IsJsonFile(fileName) != isJsonProject;
    }

    /// <summary>True for a Gum project file in either format (.gumx or .gumj).</summary>
    public static bool IsProject(string fileName) => HasExtension(fileName, GumProjectSave.ProjectExtension);

    /// <summary>True if the Gum project file is in the JSON format (.gumj).</summary>
    public static bool IsJsonProject(string gumProjectFileName) =>
        !string.IsNullOrEmpty(gumProjectFileName) && GumProjectSave.IsJsonFormat(gumProjectFileName);

    /// <summary>
    /// Returns the XML or JSON spelling of a Gum extension, regardless of which spelling is passed in.
    /// </summary>
    public static string ForFormat(string extension, bool isJson)
    {
        var xml = Normalize(extension);
        if (!isJson || string.IsNullOrEmpty(xml))
        {
            return xml;
        }

        return xml.Substring(0, xml.Length - 1) + "j";
    }
}
