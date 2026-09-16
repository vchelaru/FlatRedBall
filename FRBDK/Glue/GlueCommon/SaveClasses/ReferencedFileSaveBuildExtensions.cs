using System.IO;
using FlatRedBall.Glue.Managers;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class ReferencedFileSaveBuildExtensions
    {
        public static bool GetIsFileOutOfDate(this ReferencedFileSave instance, string absoluteSourceName, string absoluteDestinationName)
        {
            bool exists = File.Exists(absoluteDestinationName);

            if (!exists || File.GetLastWriteTime(absoluteSourceName) >
                    File.GetLastWriteTime(absoluteDestinationName))
            {
                return true;
            }

            var buildToolProcessed = BuildToolAssociationCore.Self.GetBuildToolProcessed(instance);

            if (buildToolProcessed != null)
            {
                string absoluteBuildTool = GlueStateCore.Self.CurrentMainProjectDirectory + buildToolProcessed;

                if (File.Exists(absoluteBuildTool))
                {
                    if (File.GetLastWriteTime(absoluteBuildTool) >=
                        File.GetLastWriteTime(absoluteDestinationName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
