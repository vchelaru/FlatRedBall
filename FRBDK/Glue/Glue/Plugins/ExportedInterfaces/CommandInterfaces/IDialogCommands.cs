using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IDialogCommands
    {
        #region File

        Task<ReferencedFileSave> ShowAddNewFileDialogAsync(AddNewFileViewModel viewModel = null);

        #endregion

        #region Entity

        void ShowAddNewEntityDialog();

        #endregion

        #region NamedObjectSave

        void AskToRemoveObject(NamedObjectSave namedObjectToRemove, bool saveAndRegenerate = true);


        #endregion

        void ShowAddNewScreenDialog();
        void ShowAddNewEventDialog(NamedObjectSave eventOwner);
        void ShowAddNewEventDialog(AddEventViewModel viewModel);
        void ShowLoadProjectDialog();
        void ShowAddNewCategoryDialog();

        void ShowMessageBox(string message);
        System.Windows.MessageBoxResult ShowYesNoMessageBox(string message, Action yesAction = null, Action noAction = null, string caption = "Confirm");

        void FocusTab(string dialogTitle);

        NamedObjectSave ShowAddNewObjectDialog(FlatRedBall.Glue.ViewModels.AddObjectViewModel addObjectViewModel = null);

        void ShowAddNewVariableDialog(Controls.CustomVariableType variableType = Controls.CustomVariableType.Exposed, 
            string tunnelingObject = "",
            string tunneledVariableName = "", GlueElement container = null);


        void SetFormOwner(System.Windows.Forms.Form form);
        void FocusOnTreeView();
        void MoveToCursor(System.Windows.Window window);

        void GoToDefinitionOfSelection();
    }
}
