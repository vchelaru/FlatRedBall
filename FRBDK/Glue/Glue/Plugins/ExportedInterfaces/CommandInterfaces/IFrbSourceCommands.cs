using System.Threading.Tasks;
using FlatRedBall.Glue.VSHelpers.Projects;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// Linking a game project to FlatRedBall and Gum source, which the FRB Source plugin owns. The plugin assigns its
    /// implementation to <see cref="ExportedImplementations.GlueCommands.FrbSourceCommands"/> at startup.
    /// </summary>
    public interface IFrbSourceCommands
    {
        /// <summary>Whether the FRB and Gum repositories (or FRB2's) are cloned in the default location.</summary>
        bool HasFrbAndGumReposInDefaultLocation();

        /// <summary>Links the project to the source in the default location, as a TaskManager task.</summary>
        Task AddFrbSourceToDefaultLocationAsync(VisualStudioProject project);
    }
}
