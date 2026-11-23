using System.Collections.Generic;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// Gum project queries and creation, which the Gum Plugin owns. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.GumCommands"/> at startup, so the property is null when
    /// the plugin is not loaded.
    /// </summary>
    public interface IGumCommands
    {
        /// <summary>Whether the loaded Glue project has a Gum project.</summary>
        bool HasGum();

        /// <summary>Whether the asset type is a Gum type (a Gum screen, component or standard element).</summary>
        bool IsAssetTypeInfoGum(AssetTypeInfo ati);

        /// <summary>
        /// The project-relative paths of the Forms code files Gum generates, which no Glue element owns.
        /// </summary>
        IReadOnlyList<string> GetFormsGeneratedRelativePaths();

        /// <summary>Asks whether to include Forms, then creates a Gum project in the loaded Glue project.</summary>
        Task AskToCreateGumProjectAsync();

        /// <summary>
        /// Creates a Gum project in the loaded Glue project, runs as a TaskManager task, and prints an error
        /// instead of throwing.
        /// </summary>
        /// <param name="includeForms">Whether to add the Forms components.</param>
        /// <param name="askToOverwrite">Whether to ask before replacing an existing Gum project.</param>
        Task CreateGumProjectAsync(bool includeForms, bool askToOverwrite);
    }
}
