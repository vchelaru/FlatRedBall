using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;
using System;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IDialogCommands
    {
        ReferencedFileSave ShowAddNewFileDialog();
        void ShowAddNewEntityDialog();
        void ShowAddNewScreenDialog();
        void ShowAddNewEventDialog(NamedObjectSave eventOwner);
        void ShowAddNewEventDialog(AddEventViewModel viewModel);

        void ShowMessageBox(string message);
        void ShowYesNoMessageBox(string message, Action yesAction, Action noAction = null);

        void FocusTab(string dialogTitle);

#if GLUE
        NamedObjectSave ShowAddNewObjectDialog(FlatRedBall.Glue.ViewModels.AddObjectViewModel addObjectViewModel = null);

        void ShowAddNewVariableDialog(Controls.CustomVariableType variableType = Controls.CustomVariableType.Exposed, 
            string tunnelingObject = "",
            string tunneledVariableName = "");


        void SetFormOwner(System.Windows.Forms.Form form);
        void ShowCreateDerivedScreenDialog(ScreenSave baseScreen);
#endif
    }
}
