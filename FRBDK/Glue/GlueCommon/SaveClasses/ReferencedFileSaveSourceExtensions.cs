using System;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>ReferencedFileSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows):
    /// these methods only reach <c>ObjectFinder.Self.MakeAbsoluteContent</c>, now covered by
    /// <see cref="IObjectFinderCore"/>. Lives here (net8.0, no WPF) so it and its tests can build and
    /// run on Linux/macOS. See #2276. Named differently from the original class (not a forwarding
    /// stub) to avoid a duplicate-type clash now that both assemblies are visible together via
    /// <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution
    /// doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class ReferencedFileSaveSourceExtensions
    {
        public static bool IsFileSourceForThis(this ReferencedFileSave instance, FilePath filePath)
        {
            if (!string.IsNullOrEmpty(instance.SourceFile) &&
                 new FilePath(ObjectFinderCore.Self.MakeAbsoluteContent(instance.SourceFile)) == filePath)
            {
                return true;
            }

            return false;
        }

        public static bool IsFileSourceForThis(this ReferencedFileSave instance, string fileName)
        {
            if (!string.IsNullOrEmpty(instance.SourceFile) &&
                 FileManager.RemoveDotDotSlash(ObjectFinderCore.Self.MakeAbsoluteContent(instance.SourceFile)).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
