using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace GameCommunicationPlugin.CodeGeneration;

/// <summary>The Game Communication Plugin's <see cref="IGameCommunicationCommands"/>.</summary>
internal class GameCommunicationCommands : IGameCommunicationCommands
{
    public void AddReflectionStateAssignmentCode(ICodeBlock codeBlock, StateSaveCategory category) =>
        ReflectionBasedStateCodeGenerator.AddReflectionStateAssignmentCode(codeBlock, category);
}
