using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces
{
    public interface IGlueCommands
    {
        void CloseGlue();

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
