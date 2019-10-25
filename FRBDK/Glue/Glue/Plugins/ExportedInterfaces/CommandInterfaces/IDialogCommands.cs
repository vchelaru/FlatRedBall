using FlatRedBall.Glue.SaveClasses;
using System;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IDialogCommands
    {
        ReferencedFileSave ShowAddNewFileDialog();
        void ShowAddNewEntityDialog();
        void ShowAddNewScreenDialog();

        void ShowMessageBox(string message);
        void ShowYesNoMessageBox(string message, Action yesAction, Action noAction = null);

#if GLUE
        NamedObjectSave ShowAddNewObjectDialog(FlatRedBall.Glue.ViewModels.AddObjectViewModel addObjectViewModel = null);

        void ShowAddNewVariableDialog(Controls.CustomVariableType variableType = Controls.CustomVariableType.Exposed, 
            string tunnelingObject = "",
            string tunneledVariableName = "");


        void SetFormOwner(System.Windows.Forms.Form form);
#endif
    }
}
