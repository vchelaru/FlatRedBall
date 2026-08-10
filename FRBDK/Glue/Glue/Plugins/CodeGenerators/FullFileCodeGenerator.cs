using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.CodeGenerators
{
    /// <summary>
    /// Base class for a code generator responsible for generating a stand-alone file. This is typically
    /// used to inject utility classes or runtime objects.
    /// </summary>
    public abstract class FullFileCodeGenerator
    {
        public abstract string RelativeFile { get; }

        /// <summary>
        /// Where <see cref="GenerateAndSave"/> writes, for callers that need to remove the file from
        /// the project when the feature it belongs to is turned off.
        /// </summary>
        public FilePath FileLocation => GlueState.Self.CurrentGlueProjectDirectory + RelativeFile;

        public void GenerateAndSave()
        {
            TaskManager.Self.Add(() =>
            {
                var contents = GenerateFileContents();

                FilePath fullPath = FileLocation;

                GlueCommands.Self.TryMultipleTimes(() =>
                {
                    GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(fullPath);
                    GlueCommands.Self.FileCommands.SaveIfDiffers(fullPath, contents);

                });

                AfterSave();

            }, $"Adding {RelativeFile}");
        }

        protected abstract string GenerateFileContents();

        /// <summary>
        /// Runs after the file has been written, for cleanup a generator needs to do alongside it -
        /// typically removing an earlier file this one replaced.
        /// </summary>
        protected virtual void AfterSave() { }
    }
}
