using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.FrbSourcePlugin.ViewModels
{
    public class AddFrbSourceViewModel : ViewModel
    {
        public Visibility AlreadyLinkedMessageVisibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }
        public string FrbRootFolder { get; set; }
        public string GumRootFolder { get; set; }
        public bool IncludeGumSkia { get; set; }
    }
}
