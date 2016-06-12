using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IDialogCommands
    {
        ReferencedFileSave ShowAddNewFileDialog();

        NamedObjectSave ShowAddNewObjectDialog(AddObjectViewModel addObjectViewModel = null);

        void SetFormOwner(Form form);
    }
}
