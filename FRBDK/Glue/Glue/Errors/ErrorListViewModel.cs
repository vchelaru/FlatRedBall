using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FlatRedBall.Glue.Errors
{
    public class ErrorListViewModel : ViewModel
    {
        #region Fields/Properties

        public ErrorViewModel SelectedError
        {
            get => Get<ErrorViewModel>(); 
            set => Set(value); 
        }

        public ObservableCollection<ErrorViewModel> Errors { get; private set; }

        public bool IsOutputErrorCheckingDetailsChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        //public bool IsAutoamticErrorCheckingEnabled
        //{
        //    get => Get<bool>();
        //    set => Set(value);
        //}


        

        #endregion

        public ICommand CopySingleCommand
        {
            get;
            internal set;
        }

        public ICommand CopyAllCommand
        {
            get;
            internal set;
        }

        public ICommand RefreshCommand
        {
            get; internal set;
        }


        public event EventHandler RefreshClicked;
        public ErrorListViewModel()
        {
            Errors = new ObservableCollection<ErrorViewModel>();

            CopySingleCommand = new Command(HandleCopySingle);
            CopyAllCommand = new Command(HandleCopyAll);
            RefreshCommand = new Command(() => RefreshClicked?.Invoke(this, null));
        }

        private void HandleCopySingle()
        {
            var whatToCopy = SelectedError?.Details;
            if(!string.IsNullOrEmpty(whatToCopy))
            {
                Clipboard.SetText(whatToCopy);
            }
        }

        private void HandleCopyAll()
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach(var item in Errors)
            {
                stringBuilder.AppendLine(item.Details);
            }

            var whatToCopy = stringBuilder.ToString();

            if (!string.IsNullOrEmpty(whatToCopy))
            {
                Clipboard.SetText(whatToCopy);
            }
        }
    }
}
