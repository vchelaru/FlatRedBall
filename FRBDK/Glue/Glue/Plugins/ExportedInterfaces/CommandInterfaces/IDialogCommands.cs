using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IDialogCommands
    {
        ReferencedFileSave ShowAddNewFileDialog();

        void SetFormOwner(Form form);
    }
}
