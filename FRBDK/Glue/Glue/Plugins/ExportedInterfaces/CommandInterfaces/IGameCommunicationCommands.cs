using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// Live-edit code generation, which the Game Communication Plugin owns. The plugin assigns its implementation
    /// to <see cref="ExportedImplementations.GlueCommands.GameCommunicationCommands"/> at startup, so the property
    /// is null when the plugin is not loaded.
    /// </summary>
    public interface IGameCommunicationCommands
    {
        /// <summary>
        /// Adds code to a state category's setter that applies states added while the game runs. Adds nothing when
        /// game communication is off.
        /// </summary>
        void AddReflectionStateAssignmentCode(ICodeBlock codeBlock, StateSaveCategory category);
    }
}
