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

        void CloseGlueProject(bool shouldSave = true, bool isExiting = false, GlueFormsCore.Controls.InitializationWindowWpf initWindow = null);

        void DoOnUiThread(Action action);
        void TryMultipleTimes(Action action, int numberOfTimesToTry, int msSleepBetweenAttempts = 200);

        void PrintOutput(string output);
        void PrintError(string output);

        int CompareFileSort(string first, string second);

        string GetAbsoluteFileName(SaveClasses.ReferencedFileSave rfs);
        FilePath GetAbsoluteFilePath(SaveClasses.ReferencedFileSave rfs);
        string GetAbsoluteFileName(string relativeFileName, bool isContent);
        Task LoadProjectAsync(string fileName);
        void Undo();

    }
}
