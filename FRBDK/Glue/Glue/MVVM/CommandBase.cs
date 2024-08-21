using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FlatRedBall.Glue.MVVM
{
    public class CommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged;

        Action execute;
        Func<bool> canExecuteFunc;

        public CommandBase(Action execute, Func<bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecuteFunc = canExecute;
        }

        public CommandBase(Action execute, string canExecuteProperty, INotifyPropertyChanged viewModel)
        {
            this.execute = execute;

            this.canExecuteFunc = () => viewModel?.GetType().GetProperty(canExecuteProperty, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(viewModel) is true;

            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == canExecuteProperty)
                {
                    NotifyCanExecuteChanged();
                }
            };
        }

        public bool CanExecute(object parameter)
        {
            if(canExecuteFunc != null)
            {
                return canExecuteFunc();

            }
            else
            {
                return true;
            }
        }

        public void Execute(object parameter)
        {
            if(execute != null)
            {
                execute();
            }
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, null);
        }
    }

    public class CommandBase<T> : ICommand
    {
        public event EventHandler CanExecuteChanged;

        Action<T> execute;
        Func<T,bool> canExecuteFunc;

        public CommandBase(Action<T> execute, Func<bool> canExecute = null) : this(execute, canExecute is null ? null : _ => canExecute())
        {
        }

        public CommandBase(Action<T> execute, Func<T,bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecuteFunc = canExecute;
        }

        public CommandBase(Action<T> execute, string canExecuteProperty, INotifyPropertyChanged viewModel)
        {

            this.execute = execute;

            this.canExecuteFunc = _ => viewModel?.GetType().GetProperty(canExecuteProperty, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(viewModel) is true;

            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == canExecuteProperty)
                {
                    NotifyCanExecuteChanged();
                }
            };
        }

        public CommandBase(Action<T> execute, string canExecuteMethod, string canExecuteChangedDependency, INotifyPropertyChanged viewModel)
        {
            this.execute = execute;

            this.canExecuteFunc = x => viewModel?.GetType().GetMethod(canExecuteMethod, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(viewModel, new object[] { x }) is true;

            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == canExecuteChangedDependency)
                {
                    NotifyCanExecuteChanged();
                }
            };
        }

        public bool CanExecute(object parameter)
        {
            if (canExecuteFunc != null && parameter is T p)
            {
                return canExecuteFunc(p);

            }
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is T p)
            {
                execute(p);
            }
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, null);
        }
    }
}
