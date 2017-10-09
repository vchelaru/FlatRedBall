using FlatRedBall.Glue.SaveClasses;


namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IDialogCommands
    {
        ReferencedFileSave ShowAddNewFileDialog();

        void ShowMessageBox(string message);


#if GLUE
        NamedObjectSave ShowAddNewObjectDialog(FlatRedBall.Glue.ViewModels.AddObjectViewModel addObjectViewModel = null);
        void SetFormOwner(System.Windows.Forms.Form form);
#endif
    }
}
