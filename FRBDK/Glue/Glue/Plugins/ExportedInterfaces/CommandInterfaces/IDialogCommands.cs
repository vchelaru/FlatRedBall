using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueFormsCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;

public interface IDialogCommands
{
    #region File

    Task ShowAddExistingFileDialog();

    Task<ReferencedFileSave> ShowAddNewFileDialogAsync(AddNewFileViewModel viewModel = null, GlueElement element = null);

    #endregion

    #region Entity

    void ShowAddNewEntityDialog(AddEntityViewModel viewModel = null);

    AddEntityViewModel CreateAddNewEntityViewModel();

    /// <summary>
    /// Shows the single delete dialog for an Entity and, if confirmed, removes it. Returns whether it was
    /// removed. See GitHub issue #429 - this replaced a chain of separate prompts.
    /// </summary>
    Task<bool> AskToRemoveEntityAsync(EntitySave entityToRemove, bool askToDeleteFiles = true);

    #endregion

    #region NamedObjectSave

    /// <summary>
    /// Shows the single delete dialog for an object and, if confirmed, removes it. Returns whether it was
    /// removed. See GitHub issue #2032 - this replaced <c>RemoveObjectWindow</c>.
    /// </summary>
    Task<bool> AskToRemoveObjectAsync(NamedObjectSave namedObjectToRemove, bool saveAndRegenerate = true);

    Task<bool> AskToRemoveObjectListAsync(List<NamedObjectSave> namedObjectsToRemove, bool saveAndRegenerate = true);


    Task<NamedObjectSave> ShowAddNewObjectDialog(FlatRedBall.Glue.ViewModels.AddObjectViewModel addObjectViewModel = null);

    #endregion

    #region Screen

    void ShowAddNewScreenDialog(AddScreenViewModel viewModel = null);

    /// <summary>
    /// Shows the single delete dialog for a Screen and, if confirmed, removes it. Returns whether it was
    /// removed. See GitHub issue #429 - this replaced a chain of separate prompts.
    /// </summary>
    Task<bool> AskToRemoveScreenAsync(ScreenSave screenToRemove, bool askToDeleteFiles = true);

    #endregion

    #region State

    /// <summary>
    /// Shows the single delete dialog for a state and, if confirmed, removes it. Returns whether it was
    /// removed. See GitHub issue #2032 - this replaced a yes/no plus a popup per orphaned variable.
    /// </summary>
    Task<bool> AskToRemoveStateAsync(StateSave stateToRemove);

    Task<bool> AskToRemoveStateCategoryAsync(StateSaveCategory categoryToRemove);

    #endregion

    #region CustomVariable

    Task<bool> AskToRemoveCustomVariableAsync(CustomVariable variableToRemove, bool askToDeleteFiles = true);

    #endregion

    #region Files

    /// <summary>
    /// Shows the single delete dialog for a file and, if confirmed, removes it. Returns whether it was
    /// removed. See GitHub issue #2032 - this replaced a confirm, one prompt per object using the file, and
    /// a leftover-files dialog.
    /// </summary>
    Task<bool> AskToRemoveReferencedFileAsync(ReferencedFileSave fileToRemove, bool askToDeleteFiles = true);

    #endregion

    #region Event

    void ShowAddNewEventDialog(NamedObjectSave eventOwner);
    void ShowAddNewEventDialog(AddEventViewModel viewModel);
    void ShowAddNewEventDialog(GlueElement glueElement);

    #endregion

    void ShowLoadProjectDialog();

    #region StateSave

    void ShowAddNewStateDialog();


    #endregion

    #region StateSaveCategory

    void ShowAddNewCategoryDialog();

    #endregion

    #region Message Box

    void ShowMessageBox(string message, string caption = "");
    System.Windows.MessageBoxResult ShowYesNoMessageBox(string message, string caption = "Confirm", Action yesAction = null, Action noAction = null);

    #endregion

    void FocusTab(string dialogTitle);

    #region Variable

    void ShowAddNewVariableDialog(Controls.CustomVariableType variableType = Controls.CustomVariableType.Exposed, 
        string tunnelingObject = "",
        string tunneledVariableName = "", GlueElement container = null);
    #endregion

    #region Spinners

    void ShowSpinner(string text);
    void HideSpinner();

    #endregion

    #region Toast

    /// <summary>
    /// Shows toast for the argument amount of time. If null, the default time is used.
    /// Calling this multiple times changes the text and resets the timer.
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <param name="timeToShowToast">The amount of time. If null, the default time is used.</param>
    void ShowToast(string text, TimeSpan? timeToShowToast = null);

    void HideToast();

    #endregion

    #region General IO

    (DialogResult DialogResult, FilePath? FilePath) ShowSaveDialog(string filter);

    #endregion

    void SetFormOwner(System.Windows.Forms.Form form);
    void FocusOnTreeView();
    void MoveToCursor(System.Windows.Window window);

    bool IsModalWindowOpen();


    void GoToDefinitionOfSelection();
}
