using FlatRedBall.Glue.IO;
using Gum.DataTypes;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GumPlugin.Managers
{
    /// <summary>
    /// Reads the .gumx and its element files on a background thread while Glue starts, the way
    /// <see cref="ProjectLoader.StartEarlyReads"/> reads the .gluj, so the project load takes the parsed project
    /// instead of reading it. The load takes it only for the same .gumx with no Gum data file changed since the
    /// read started; otherwise it reads the files itself as before. Glue.exe and GlueCli both start it.
    /// The read only parses files into a new <see cref="GumProjectSave"/> (Gum's serializer caches are locked);
    /// nothing shared, such as Gum's ObjectFinder, sees it until the load assigns it on the load's thread.
    /// </summary>
    public class GumProjectPrefetch
    {
        sealed record Read(string GumxFile, int Stamp, GumProjectSave Project, GumLoadResult Result);

        Task<Read> pending;

        /// <summary>Finishes when the read does; for tests that change the files after it.</summary>
        internal Task Completion => pending ?? Task.CompletedTask;

        /// <summary>
        /// Starts reading the .gumx the early-read project lists in Global Content. Does nothing without an early
        /// read. The .gumx is looked for under the .csproj's Content folder, where desktop projects keep it.
        /// </summary>
        public void Start(EarlyProjectFiles earlyFiles)
        {
            if (earlyFiles == null)
            {
                return;
            }

            var contentDirectory = Path.Combine(Path.GetDirectoryName(earlyFiles.CsprojFullPath), "Content");

            pending = Task.Run(async () =>
            {
                try
                {
                    var globalFiles = await earlyFiles.GlobalFileNames;
                    var gumxName = globalFiles.FirstOrDefault(GumFileExtensions.IsProject);
                    if (gumxName == null)
                    {
                        return null;
                    }

                    var gumxFile = Path.GetFullPath(Path.Combine(contentDirectory, gumxName));
                    if (!File.Exists(gumxFile))
                    {
                        return null;
                    }

                    // Taken before the read, so a file written during it changes the stamp the load compares.
                    var stamp = FileReferenceTracker.GetGumDataStamp(Path.GetDirectoryName(gumxFile));
                    var project = GumProjectSave.Load(gumxFile, out var result);
                    return new Read(gumxFile, stamp, project, result);
                }
                catch (Exception)
                {
                    // The load reads the files itself and reports whatever went wrong.
                    return null;
                }
            });
        }

        /// <summary>
        /// The prefetched project if it was read from <paramref name="gumxFile"/> and the Gum data stamp is still
        /// <paramref name="currentStamp"/>. Whatever the outcome, the prefetch is used up.
        /// </summary>
        public bool TryTake(string gumxFile, int currentStamp, out GumProjectSave project, out GumLoadResult result)
        {
            project = null;
            result = null;

            var taken = pending;
            pending = null;
            if (taken == null)
            {
                return false;
            }

            var read = taken.GetAwaiter().GetResult();
            if (read == null || read.Stamp != currentStamp ||
                !string.Equals(read.GumxFile, Path.GetFullPath(gumxFile), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            project = read.Project;
            result = read.Result;
            return true;
        }
    }
}
