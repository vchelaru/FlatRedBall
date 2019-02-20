using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FlatRedBall.Glue.Errors
{
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
}
