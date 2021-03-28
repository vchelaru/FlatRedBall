using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.IO;
using System;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces
{
    public interface IGlueCommands
    {
        IGenerateCodeCommands GenerateCodeCommands { get; }
        IGluxCommands GluxCommands { get; }
        IProjectCommands ProjectCommands { get; }
        IRefreshCommands RefreshCommands { get; }
        ITreeNodeCommands TreeNodeCommands { get; }
        IUpdateCommands UpdateCommands { get; }
        IDialogCommands DialogCommands { get; }
        IFileCommands FileCommands { get; }

        void CloseGlue();

        void TryMultipleTimes(Action action, int numberOfTimesToTry);

        void PrintOutput(string output);
        void PrintError(string output);


        string GetAbsoluteFileName(SaveClasses.ReferencedFileSave rfs);
        FilePath GetAbsoluteFilePath(SaveClasses.ReferencedFileSave rfs);
        string GetAbsoluteFileName(string relativeFileName, bool isContent);
        void LoadProject(string fileName);
        Task LoadProjectAsync(string fileName);

    }
}
