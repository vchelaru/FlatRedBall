using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OfficialPlugins.ErrorPlugin.ViewModels
{
    #region Command Implementation

    public class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public Action ExecuteAction { get; set; }

        public Command(Action executeAction)
        {
            this.ExecuteAction = executeAction;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            ExecuteAction?.Invoke();
        }
    }

    #endregion

    public class ErrorViewModel : ViewModel
    {
        public string Details
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public ErrorViewModel()
        {
            DoubleClickCommand = new Command(() => HandleDoubleClick());
        }

        public ICommand DoubleClickCommand
        {
            get;
            internal set;
        }

        public virtual void HandleDoubleClick()
        {

        }

        public virtual bool ReactsToFileChange(FilePath filePath)
        {
            return false;
        }

        public virtual bool GetIfIsFixed()
        {
            return false;
        }

        public override string ToString()
        {
            return Details;
        }
    }
}
