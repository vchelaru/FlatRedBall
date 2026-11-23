using System.Threading.Tasks;
using ToolsUtilities;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// Building and running the game, which the Compiler Plugin owns. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.CompilerCommands"/> at startup, so the property is null
    /// when the plugin is not loaded.
    /// </summary>
    public interface ICompilerCommands
    {
        /// <summary>Adds a line to the Build tab's output. Safe to call from any thread.</summary>
        void PrintOutput(string output);

        /// <summary>Builds the game project. Never throws: a failure comes back as an unsuccessful response.</summary>
        Task<CompileGeneralResponse> CompileAsync(string configuration, bool printMsBuildCommand);

        /// <summary>Runs the last build of the game.</summary>
        /// <param name="preventFocus">Whether to keep focus on Glue instead of the game window.</param>
        /// <param name="runArguments">Command-line arguments for the game.</param>
        Task<GeneralResponse> RunAsync(bool preventFocus, string runArguments);

        Task KillGameProcessAsync();
    }
}
