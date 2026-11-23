using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// Running the game from the editor and talking to it while it runs, which the Glue Compiler plugin (in the
    /// Game Communication Plugin) owns. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.GameHostCommands"/> at startup, so the property is null
    /// when the plugin is not loaded.
    /// </summary>
    public interface IGameHostCommands
    {
        /// <summary>Whether the game is running, in play or edit mode.</summary>
        bool IsGameRunning();

        /// <summary>
        /// Builds and, if the build succeeds, runs the game, as the toolbar's play button does. Does nothing while
        /// the game is running or building. Runs as a TaskManager task and prints an error instead of throwing.
        /// </summary>
        Task BuildAndRunAsync();

        /// <summary>
        /// Shows a state of the current element in the running game. Prints an error instead of throwing.
        /// </summary>
        Task ShowStateAsync(string categoryName, string stateName);

        /// <summary>Sets whether commands sent to the game are printed to the Build tab.</summary>
        void SetIsLoggingSentCommands(bool isLogging);

        /// <summary>Sets whether commands received from the game are printed to the Build tab.</summary>
        void SetIsLoggingReceivedCommands(bool isLogging);
    }
}
