using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using System;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces
{
    public interface IGlueCommands
    {
        void CloseGlue();

        void TryMultipleTimes(Action action, int numberOfTimesToTry);

        IGenerateCodeCommands GenerateCodeCommands { get; }
        IGluxCommands GluxCommands { get; }
        IOpenCommands OpenCommands { get; }
        IProjectCommands ProjectCommands { get; }
        IRefreshCommands RefreshCommands { get; }
        ITreeNodeCommands TreeNodeCommands { get; }
        IUpdateCommands UpdateCommands { get; }
        IDialogCommands DialogCommands { get; }
        GlueViewCommands GlueViewCommands { get; }
    }
}
