using FlatRedBall.Forms.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormsSampleProject.ViewModels
{
    public class MainMenuViewModel : ViewModel
    {
        public ObservableCollection<string> ComboBoxItems { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ListBoxItems { get; set; } = new ObservableCollection<string>();
        
    }
}
