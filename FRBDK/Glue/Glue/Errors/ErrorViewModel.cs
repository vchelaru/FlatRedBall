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
