using FlatRedBall.Glue.SaveClasses;
using System;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IDialogCommands
    {
        ReferencedFileSave ShowAddNewFileDialog();
        void ShowAddNewEntityDialog();

        void ShowMessageBox(string message);
        void ShowYesNoMessageBox(string message, Action yesAction, Action noAction = null);

#if GLUE
        NamedObjectSave ShowAddNewObjectDialog(FlatRedBall.Glue.ViewModels.AddObjectViewModel addObjectViewModel = null);
        void SetFormOwner(System.Windows.Forms.Form form);
#endif
    }
}
