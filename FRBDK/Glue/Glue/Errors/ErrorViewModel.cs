using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FlatRedBall.Glue.Errors
{
    public class MenuItemViewModel
    {
        public string Header { get; set; }
        public ICommand Command { get; set; }
    }

    /// <summary>
    /// Base class for reporting an error in Glue. For implementation examples, see the 
    /// IErrorReporter interface.
    /// </summary>
    public abstract class ErrorViewModel : ViewModel
    {

        public ObservableCollection<MenuItemViewModel> MenuItemList { get; set; }

        public abstract string UniqueId
        {
            get;
        }


        public string Details
        {
            get => Get<string>(); 
            set => Set(value); 
        }

        public string DetailsDisplay => Details ?? this.GetType().Name;

        public ErrorViewModel()
        {
            MenuItemList = new ObservableCollection<MenuItemViewModel>();
            DoubleClickCommand = new Command(() => HandleDoubleClick());

            MenuItemList.Add(new MenuItemViewModel { Header = "Go To Object", Command = DoubleClickCommand });

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
