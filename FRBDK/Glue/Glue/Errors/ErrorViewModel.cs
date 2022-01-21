using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FlatRedBall.Glue.Errors
{
    /// <summary>
    /// Base class for reporting an error in Glue. For implementation examples, see the 
    /// IErrorReporter interface.
    /// </summary>
    public abstract class ErrorViewModel : ViewModel
    {
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
