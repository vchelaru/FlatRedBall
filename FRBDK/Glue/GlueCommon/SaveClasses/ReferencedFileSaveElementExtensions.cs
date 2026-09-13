using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>ReferencedFileSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows):
    /// these methods are pure logic over a <see cref="ReferencedFileSave"/>, but need to resolve the
    /// element containing it, so they depend on <see cref="IObjectFinderCore"/> (a narrow seam over
    /// <c>ObjectFinder.Self</c>, see that interface's doc comment) instead of reaching for
    /// <c>ObjectFinder.Self</c> directly. Lives here (net8.0, no WPF) so it and its tests can build and
    /// run on Linux/macOS. See issue #2276. Named differently from the original class (not a forwarding
    /// stub) to avoid a duplicate-type clash now that both assemblies are visible together via
    /// <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution
    /// doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class ReferencedFileSaveElementExtensions
    {
        public static GlueElement GetContainer(this ReferencedFileSave instance)
        {
            if (ObjectFinderCore.Self.GlueProject != null)
            {
                return ObjectFinderCore.Self.GetElementContaining(instance);
            }
            else
            {
                return null;
            }
        }

        public static ContainerType GetContainerType(this ReferencedFileSave instance)
        {
            IElement element = instance.GetContainer();

            if (element != null)
            {
                if (element is ScreenSave)
                {
                    return ContainerType.Screen;
                }
                else
                {
                    return ContainerType.Entity;
                }
            }
            else
            {
                return ContainerType.None;
            }
        }

        public static string ReferencedFileSaveToString(ReferencedFileSave instance)
        {
            string containerText = "";
            if (instance.GetContainerType() == ContainerType.None)
            {
                containerText = " (in GlobalContent)";
            }
            else
            {
                containerText = " (in " + instance.GetContainer() + ")";
            }
            return instance.Name + containerText;
        }

        /// <summary>
        /// Whether IsSharedStatic has any effect for this file, and so should be shown as an editable
        /// property. It only varies per-instance for Screen-owned files - Entity-owned files are forced
        /// static (unique-instance optimization) and global content is always static (its code generator
        /// never reads the flag) - see the comment in ReferencedFileSave's constructor.
        /// </summary>
        public static bool GetIsSharedStaticEditable(this ReferencedFileSave instance) =>
            instance.GetContainerType() == ContainerType.Screen;

        /// <summary>
        /// Whether the file lives outside the content folder owned by its container - a Screen or Entity
        /// file that isn't under that element's own folder, or a global file that isn't under GlobalContent.
        /// Such a file is a reference to content that lives somewhere else (another element's folder,
        /// GlobalContent, or loose in the Content folder), so the tree view marks it with a link icon.
        /// </summary>
        public static bool GetIsLinkedOutsideContainerFolder(this ReferencedFileSave instance) =>
            GetIsFileOutsideContainerFolder(instance?.Name, instance?.GetContainer()?.Name);

        /// <summary>
        /// GetIsLinkedOutsideContainerFolder for a caller that already knows the container (null for global
        /// content), so it doesn't pay for an ObjectFinder search to rediscover it.
        /// </summary>
        public static bool GetIsLinkedOutsideContainerFolder(this ReferencedFileSave instance, GlueElement container) =>
            GetIsFileOutsideContainerFolder(instance?.Name, container?.Name);

        /// <summary>
        /// The container-folder comparison behind GetIsLinkedOutsideContainerFolder, split out so it can be
        /// tested and called without an ObjectFinder lookup.
        /// </summary>
        /// <param name="referencedFileName">The ReferencedFileSave's Name - a path relative to the content
        /// project, like "Entities/Player/PlayerSheet.png".</param>
        /// <param name="containerName">The owning GlueElement's Name, like "Entities\Player", or null for
        /// global content.</param>
        public static bool GetIsFileOutsideContainerFolder(string referencedFileName, string containerName)
        {
            if (string.IsNullOrWhiteSpace(referencedFileName))
            {
                return false;
            }

            // Element names use backslashes ("Entities\Player") while file names use forward slashes, and
            // an element's content folder is its name - see ElementCommands.GetFullPathContentDirectory.
            var containerFolder = string.IsNullOrWhiteSpace(containerName)
                ? "GlobalContent"
                : containerName;

            containerFolder = containerFolder.Replace('\\', '/').TrimEnd('/') + "/";

            return !referencedFileName.Replace('\\', '/')
                .StartsWith(containerFolder, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the name of (property) variable generated in code. This does not return the field.
        /// </summary>
        /// <param name="instance">The ReferencedFileSave to generate.</param>
        /// <returns>The name as generated.</returns>
        public static string GetInstanceName(this ReferencedFileSave instance)
        {
            if (instance.CachedInstanceName == null)
            {
                instance.CachedInstanceName =
                    FileManager.RemoveExtension(instance.Name)
                        .Replace(" ", "")
                        .Replace("-", "_")
                        // May 17, 2022
                        // File names with
                        // invalid characters
                        // may make their way in
                        // to a project. In this case
                        // we will just strip out the invalid
                        // characters. We treat the dash as a special
                        // case since it can be converted to an underscore
                        // and still look somewhat similar.
                        .Replace("(", "")
                        .Replace(")", "");

                if (instance.IncludeDirectoryRelativeToContainer)
                {
                    IElement container = instance.GetContainer();

                    if (container != null)
                    {
                        string directoryToMakeRelativeTo = container.Name;

                        bool isRelativeTo = FileManager.IsRelativeTo(instance.CachedInstanceName, directoryToMakeRelativeTo);

                        if (isRelativeTo)
                        {
                            instance.CachedInstanceName = FileManager.MakeRelative(instance.CachedInstanceName, directoryToMakeRelativeTo);
                            instance.CachedInstanceName = instance.CachedInstanceName.Replace("/", "_");
                        }
                        else
                        {
                            instance.CachedInstanceName = FileManager.RemovePath(instance.CachedInstanceName);
                        }
                    }
                    else
                    {
                        // Might be something like:  "GlobalContent/FolderInGlobalContent/SceneFileInFolder"
                        if (instance.CachedInstanceName.StartsWith("GlobalContent/"))
                        {
                            instance.CachedInstanceName = instance.CachedInstanceName.Substring("GlobalContent/".Length);
                        }
                        instance.CachedInstanceName = instance.CachedInstanceName.Replace("/", "_");
                    }
                }
                else
                {
                    instance.CachedInstanceName = FileManager.RemovePath(instance.CachedInstanceName);
                }

                if (instance.CachedInstanceName.Length > 0 && char.IsDigit(instance.CachedInstanceName[0]))
                {
                    instance.CachedInstanceName = '_' + instance.CachedInstanceName;
                }
            }

            return instance.CachedInstanceName;
        }
    }
}
