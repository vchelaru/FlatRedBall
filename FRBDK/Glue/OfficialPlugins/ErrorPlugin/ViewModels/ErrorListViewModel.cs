using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorPlugin.ViewModels
{
    public class ErrorListViewModel : ViewModel
    {
        public ObservableCollection<ErrorViewModel> Errors { get; private set; }

        public ErrorListViewModel()
        {
            Errors = new ObservableCollection<ErrorViewModel>();
        }
    }
}
