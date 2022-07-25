using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;

namespace GameCommunicationPlugin.Common
{
    internal static class GameCommunicationHelper
    {
        public static bool IsFrbNewEnough()
        {
            var mainProject = GlueState.Self.CurrentMainProject;
            if (mainProject.IsFrbSourceLinked())
            {
                return true;
            }
            else
            {
                return GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode;
            }
        }

        internal static bool IsFrbUsesJson()
        {
            return GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SeparateJsonFilesForElements;
        }
    }
}
