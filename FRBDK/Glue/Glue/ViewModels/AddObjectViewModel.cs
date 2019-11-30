using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.ViewModels
{
    public class AddObjectViewModel : ViewModel
    {
        public DialogResult DialogResult { get; set;}
        public SourceType SourceType { get; set;}



        public string SourceClassType { get; set;}
        public string SourceFile { get; set;}
        public string ObjectName { get; set;}
        public string SourceNameInFile { get; set;}
        public string SourceClassGenericType { get; set;}

        public ObservableCollection<string> FlatRedBallAndCustomTypes { get; set; } = new ObservableCollection<string>();
    }
}
