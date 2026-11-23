using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace GlueUnitTests.Managers;

// The unreferenced-files refresh asks for the files each file needs on disk, after Glue already asked for the
// files it references. For a plugin without its own GetFilesNeededOnDiskBy those are the same files, so the
// plugin used to parse every file twice (each Gum element file was read from disk twice per load).
public class FileReferenceManagerNeededOnDiskTests : IDisposable
{
    const string ReferencingExtension = "neededondisktestref";
    const string BuiltExtension = "neededondisktestbuilt";

    readonly string directory = Path.Combine(Path.GetTempPath(), nameof(FileReferenceManagerNeededOnDiskTests) + "_" + Guid.NewGuid().ToString("N"));
    readonly FilePath file;

    public FileReferenceManagerNeededOnDiskTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        ReferencingPlugin.EnsureRegistered();
        Directory.CreateDirectory(directory);
        file = new FilePath(Path.Combine(directory, "File." + ReferencingExtension));
        File.WriteAllText(file.FullPath, "");
    }

    public void Dispose()
    {
        FileReferenceManager.Self.ClearFileCache(file);
        try { Directory.Delete(directory, recursive: true); } catch (Exception) { }
    }

    [Fact]
    public void GetFilesNeededOnDiskBy_ShouldNotAskPluginsAgain_WhenReferencedFilesAreKnown()
    {
        var referenced = FileReferenceManager.Self.GetFilesReferencedBy(file, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);
        var callsAfterReferenced = ReferencingPlugin.CallsFor(file);

        var neededOnDisk = FileReferenceManager.Self.GetFilesNeededOnDiskBy(file, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);

        ReferencingPlugin.CallsFor(file).ShouldBe(callsAfterReferenced);
        referenced.ShouldContain(ReferencingPlugin.ReferencedFileOf(file));
        neededOnDisk.ShouldContain(ReferencingPlugin.ReferencedFileOf(file));
    }

    [Fact]
    public void GetFilesNeededOnDiskBy_ShouldIncludeFilesFromPluginsWithTheirOwnHandler_WhenReferencedFilesAreKnown()
    {
        FileReferenceManager.Self.GetFilesReferencedBy(file, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);

        var neededOnDisk = FileReferenceManager.Self.GetFilesNeededOnDiskBy(file, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);

        neededOnDisk.ShouldContain(BuiltFilePlugin.BuiltFileOf(file));
        neededOnDisk.ShouldContain(ReferencingPlugin.ReferencedFileOf(file));
    }

    [Fact]
    public void GetFilesNeededOnDiskBy_ShouldAskPlugins_WhenReferencedFilesAreNotKnown()
    {
        var neededOnDisk = FileReferenceManager.Self.GetFilesNeededOnDiskBy(file, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);

        ReferencingPlugin.CallsFor(file).ShouldBe(1);
        neededOnDisk.ShouldContain(ReferencingPlugin.ReferencedFileOf(file));
        neededOnDisk.ShouldContain(BuiltFilePlugin.BuiltFileOf(file));
    }

    // Reports one referenced file per file with ReferencingExtension, and counts how often it was asked.
    class ReferencingPlugin : PluginBase
    {
        static bool registered;
        static readonly Dictionary<FilePath, int> calls = new();

        public static void EnsureRegistered()
        {
            if (!registered)
            {
                PluginManager.RegisterPlugin(new ReferencingPlugin());
                PluginManager.RegisterPlugin(new BuiltFilePlugin());
                registered = true;
            }
        }

        public static int CallsFor(FilePath filePath)
        {
            lock (calls) { return calls.TryGetValue(filePath, out var count) ? count : 0; }
        }

        public static FilePath ReferencedFileOf(FilePath filePath) => filePath.RemoveExtension() + ".png";

        public override string FriendlyName => nameof(FileReferenceManagerNeededOnDiskTests) + nameof(ReferencingPlugin);
        public override Version Version => new Version(1, 0);

        public override void StartUp()
        {
            FillWithReferencedFiles = (filePath, list) =>
            {
                if (filePath.Extension == ReferencingExtension)
                {
                    lock (calls) { calls[filePath] = CallsFor(filePath) + 1; }
                    list.Add(ReferencedFileOf(filePath));
                }
                return GeneralResponse.SuccessfulResponse;
            };
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }

    // Like the content pipeline plugin: adds a built file through its own GetFilesNeededOnDiskBy.
    class BuiltFilePlugin : PluginBase
    {
        public static FilePath BuiltFileOf(FilePath filePath) => filePath.RemoveExtension() + "." + BuiltExtension;

        public override string FriendlyName => nameof(FileReferenceManagerNeededOnDiskTests) + nameof(BuiltFilePlugin);
        public override Version Version => new Version(1, 0);

        public override void StartUp()
        {
            GetFilesNeededOnDiskBy = (fileName, list) =>
            {
                var filePath = new FilePath(fileName);
                if (filePath.Extension == ReferencingExtension)
                {
                    list.Add(BuiltFileOf(filePath));
                }
            };
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
