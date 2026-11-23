using System.Threading.Tasks;
using FlatRedBall.Glue.VSHelpers.Projects;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// The MonoGame Content plugin, which builds .xnb files. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.ContentPipelineCommands"/> at startup.
    /// </summary>
    public interface IContentPipelineCommands
    {
        /// <summary>Builds the project's missing or stale .xnb files, as a TaskManager task.</summary>
        Task RefreshXnbsForProjectAsync(VisualStudioProject project);
    }
}
